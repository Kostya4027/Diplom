using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTickets.Core.DTOs;

namespace ExamTickets.Core.Services;

public class TicketGeneratorService
{
    private static readonly Random Random = new();

    public Dictionary<string, List<string>> ExtractQuestions(string filePath, List<string> markers)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Путь к файлу не задан.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл не найден.", filePath);

        if (markers is null || markers.Count == 0)
            throw new InvalidOperationException("Список маркеров не может быть пустым.");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is not ".txt" and not ".docx")
            throw new InvalidOperationException("Поддерживаются только файлы .txt и .docx.");

        var result = markers.ToDictionary(marker => marker, _ => new List<string>(), StringComparer.OrdinalIgnoreCase);

        if (extension == ".txt")
        {
            ExtractQuestionsFromTxt(filePath, markers, result);
        }
        else
        {
            ExtractQuestionsFromDocx(filePath, markers, result);
        }

        return result;
    }

    private void ExtractQuestionsFromTxt(string filePath, List<string> markers, Dictionary<string, List<string>> result)
    {
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);
        string? currentMarker = null;
        var currentQuestion = new StringBuilder();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var matchedMarker = markers.FirstOrDefault(marker =>
                string.Equals(line, marker, StringComparison.OrdinalIgnoreCase));

            if (matchedMarker is not null)
            {
                if (currentMarker is not null && currentQuestion.Length > 0)
                {
                    result[currentMarker].Add(currentQuestion.ToString().Trim());
                    currentQuestion.Clear();
                }

                currentMarker = matchedMarker;
                continue;
            }

            if (currentMarker is not null)
            {
                bool isNewQuestion = line.StartsWith("-") || 
                                     line.StartsWith("–") || 
                                     line.StartsWith("•") ||
                                     Regex.IsMatch(line, @"^\d+[\).]");

                if (isNewQuestion)
                {
                    if (currentQuestion.Length > 0)
                    {
                        result[currentMarker].Add(currentQuestion.ToString().Trim());
                        currentQuestion.Clear();
                    }
                    
                    var cleanLine = Regex.Replace(line, @"^[-–•\d]+[\).\s]*", string.Empty).Trim();
                    currentQuestion.AppendLine(cleanLine);
                }
                else
                {
                    currentQuestion.AppendLine(line);
                }
            }
        }

        if (currentMarker is not null && currentQuestion.Length > 0)
        {
            result[currentMarker].Add(currentQuestion.ToString().Trim());
        }
    }

    private void ExtractQuestionsFromDocx(string filePath, List<string> markers, Dictionary<string, List<string>> result)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null) return;

        string? currentMarker = null;
        var currentQuestion = new StringBuilder();

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var lineText = string.Concat(
                paragraph.Descendants<Text>().Select(x => x.Text)).Trim();

            var matchedMarker = markers.FirstOrDefault(m =>
                string.Equals(lineText, m, StringComparison.OrdinalIgnoreCase));

            if (matchedMarker is not null)
            {
                if (currentMarker is not null && currentQuestion.Length > 0)
                {
                    result[currentMarker].Add(currentQuestion.ToString().Trim());
                    currentQuestion.Clear();
                }
                currentMarker = matchedMarker;
                continue;
            }

            if (currentMarker is null) continue;

            if (string.IsNullOrWhiteSpace(lineText))
            {
                if (currentQuestion.Length > 0)
                {
                    result[currentMarker].Add(currentQuestion.ToString().Trim());
                    currentQuestion.Clear();
                }
                continue;
            }

            bool isListItem = paragraph.ParagraphProperties?.NumberingProperties != null;
            if (isListItem)
            {
                if (currentQuestion.Length > 0)
                {
                    result[currentMarker].Add(currentQuestion.ToString().Trim());
                    currentQuestion.Clear();
                }
                currentQuestion.AppendLine(lineText);
            }
            else
            {
                if (currentQuestion.Length > 0)
                    currentQuestion.AppendLine(lineText);
                else
                    currentQuestion.AppendLine(lineText);
            }
        }

        if (currentMarker is not null && currentQuestion.Length > 0)
        {
            result[currentMarker].Add(currentQuestion.ToString().Trim());
        }
    }

    public List<List<string>> GenerateTickets(
        Dictionary<string, List<string>> questions,
        List<string> markers,
        int count)
    {
        if (questions is null) throw new ArgumentNullException(nameof(questions));
        if (markers is null || markers.Count == 0) throw new ArgumentException("Список маркеров не может быть пустым.", nameof(markers));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Количество билетов должно быть больше нуля.");

        var maxTickets = CalculateMaxTickets(questions, markers);
        if (count > maxTickets)
        {
            throw new InvalidOperationException($"Невозможно сгенерировать {count} билетов. Максимум: {maxTickets}.");
        }

        var shuffled = markers.ToDictionary(
            marker => marker,
            marker => questions.TryGetValue(marker, out var list)
                ? list.OrderBy(_ => Random.Next()).ToList()
                : new List<string>(),
            StringComparer.OrdinalIgnoreCase);

        var tickets = new List<List<string>>(count);

        for (var ticketIndex = 0; ticketIndex < count; ticketIndex++)
        {
            var ticket = new List<string>();

            foreach (var marker in markers)
            {
                if (!shuffled.TryGetValue(marker, out var list) || list.Count <= ticketIndex)
                {
                    throw new InvalidOperationException($"Недостаточно вопросов для маркера '{marker}'.");
                }

                ticket.Add(list[ticketIndex]);
            }

            tickets.Add(ticket);
        }

        return tickets;
    }

    public bool ValidateQuestionFile(string filePath, List<string> markers)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new InvalidOperationException("Путь к файлу не задан.");

        if (!File.Exists(filePath))
            throw new InvalidOperationException($"Файл не найден: {filePath}");

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (extension is not ".txt" and not ".docx")
            throw new InvalidOperationException("Поддерживаются только файлы .txt и .docx.");

        if (markers is null || markers.Count == 0)
            throw new InvalidOperationException("Список маркеров не может быть пустым.");

        var questions = ExtractQuestions(filePath, markers);

        var required = new HashSet<string>(markers, StringComparer.OrdinalIgnoreCase);
        var found = new HashSet<string>(
            questions.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key),
            StringComparer.OrdinalIgnoreCase);

        var missingMarkers = required.Except(found).ToList();

        if (missingMarkers.Any())
        {
            throw new InvalidOperationException(
                $"В файле отсутствуют маркеры: {string.Join(", ", missingMarkers)}\n" +
                $"Убедитесь что каждый маркер ('Вопрос 1', 'Вопрос 2' и т.д.) написан на отдельной строке точно так же как указано.");
        }

        foreach (var marker in markers)
        {
            if (questions.TryGetValue(marker, out var list))
            {
                if (list.Count == 0)
                {
                    throw new InvalidOperationException($"Маркер '{marker}' найден, но под ним нет вопросов.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Маркер '{marker}' найден, но под ним нет вопросов.");
            }
        }

        return true;
    }

    public int CalculateMaxTickets(Dictionary<string, List<string>> questions, List<string> markers)
    {
        if (questions is null) throw new ArgumentNullException(nameof(questions));
        if (markers is null || markers.Count == 0) throw new ArgumentException("Список маркеров не может быть пустым.", nameof(markers));

        var minCount = int.MaxValue;

        foreach (var marker in markers)
        {
            if (!questions.TryGetValue(marker, out var list) || list.Count == 0)
            {
                return 0;
            }

            minCount = Math.Min(minCount, list.Count);
        }

        return minCount == int.MaxValue ? 0 : minCount;
    }

    public void ReplaceTextPlaceholders(OpenXmlElement root, Dictionary<string, string> replacements)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));
        if (replacements is null || replacements.Count == 0) return;

        foreach (var paragraph in root.Descendants<Paragraph>())
        {
            var texts = paragraph.Elements<Run>().SelectMany(r => r.Elements<Text>()).ToList();
            if (texts.Count == 0) continue;

            string fullText = string.Concat(texts.Select(t => t.Text));

            if (replacements.Keys.Any(k => fullText.Contains(k)))
            {
                foreach (var pair in replacements)
                {
                    fullText = fullText.Replace(pair.Key, pair.Value ?? string.Empty, StringComparison.Ordinal);
                }

                foreach (var t in texts)
                {
                    t.Text = string.Empty;
                }

                var firstRun = paragraph.Elements<Run>().FirstOrDefault();
                if (firstRun != null)
                {
                    firstRun.RemoveAllChildren<Text>();
                    firstRun.AppendChild(new Text(fullText) { Space = SpaceProcessingModeValues.Preserve });
                }
                else
                {
                    texts[0].Text = fullText;
                }
            }
        }
    }

    public bool IsFileLocked(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    public async Task<byte[]> GenerateTicketsDocumentAsync(TicketFormData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "TicketTemplate.docx");
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Шаблон TicketTemplate.docx не найден.", templatePath);
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.docx");
        File.Copy(templatePath, tempPath, true);

        await Task.Run(() =>
        {
            // Глобальная замена {educationalinstitution} через сырой XML,
            // так как Word может разбивать маркеры на несколько элементов Text.
            using (var document = WordprocessingDocument.Open(tempPath, true))
            {
                if (document.MainDocumentPart is not null)
                {
                    string docText;
                    using (var reader = new StreamReader(document.MainDocumentPart.GetStream()))
                    {
                        docText = reader.ReadToEnd();
                    }

                    // Очистка тегов (разрывов) внутри маркера (например, {educ<w:r>...ationalinstitution})
                    var regex = new Regex(@"\{[^{}]*\}");
                    docText = regex.Replace(docText, match => Regex.Replace(match.Value, @"<[^>]+>", string.Empty));

                    docText = docText.Replace("{educationalinstitution}", data.EducationalInstitution ?? string.Empty, StringComparison.OrdinalIgnoreCase);

                    using (var writer = new StreamWriter(document.MainDocumentPart.GetStream(FileMode.Create)))
                    {
                        writer.Write(docText);
                    }
                }
            }

            var markers = Enumerable.Range(1, data.QuestionsPerTicket)
                .Select(i => $"Вопрос {i}")
                .ToList();

            var questions = ExtractQuestions(data.QuestionFilePath, markers);
            var tickets = GenerateTickets(questions, markers, data.TicketCount);

            using (var document = WordprocessingDocument.Open(tempPath, true))
            {
                var body = document.MainDocumentPart?.Document?.Body;
                if (body is null)
                {
                    throw new InvalidOperationException("Шаблон документа поврежден: отсутствует тело документа.");
                }

                var tableTemplate = body.Elements<Table>().FirstOrDefault();
                if (tableTemplate is null)
                {
                    throw new InvalidOperationException("В шаблоне не найдена таблица билета.");
                }

                for (int i = 0; i < tickets.Count; i++)
                {
                    var ticket = tickets[i];
                    var clonedTable = (Table)tableTemplate.CloneNode(true);

                    var replacements = new Dictionary<string, string>
                    {
                        ["{commission}"] = data.Commission,
                        ["{protocolnumber}"] = data.ProtocolNumber,
                        ["{date}"] = data.Date.ToString("dd.MM.yyyy"),
                        ["{chairman}"] = data.Chairman,
                        ["{specialitynumber}"] = data.SpecialtyNumber,
                        ["{examtype}"] = data.ExamType,
                        ["{exam}"] = data.Exam,
                        ["{groupsnumber}"] = data.GroupsNumber,
                        ["{semester}"] = data.Semester.ToString(),
                        ["{affirmer}"] = data.Affirmer,
                        ["{affirmerlastname}"] = data.AffirmerLastName,
                        ["{dateofstatement}"] = data.DateOfStatement.ToString("dd.MM.yyyy"),
                        ["{teachers}"] = data.Teachers,
                        ["{ticketnumber}"] = (i + 1).ToString()
                    };

                    for (int q = 1; q <= 4; q++)
                    {
                        replacements[$"{{QUESTION{q}}}"] = q <= ticket.Count ? ticket[q - 1] : string.Empty;
                    }

                    ReplaceTextPlaceholders(clonedTable, replacements);
                    body.AppendChild(clonedTable);

                    if (i < tickets.Count - 1)
                    {
                        body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                    }
                }

                tableTemplate.Remove();
                document.MainDocumentPart.Document.Save();
            }
        });

        var bytes = await File.ReadAllBytesAsync(tempPath);
        File.Delete(tempPath);

        return bytes;
    }

    private static IEnumerable<string> ReadDocxLines(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);

        var body = document.MainDocumentPart?.Document.Body;
        if (body is null) yield break;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(x => x.Text));
            if (!string.IsNullOrWhiteSpace(text))
            {
                yield return text;
            }
        }
    }
}