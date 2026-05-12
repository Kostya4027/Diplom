# Техническое задание и план разработки
## «Система генерации экзаменационных билетов» (версия 3.0 — дипломный проект)

---

## ЧАСТЬ 1 — АНАЛИЗ И РЕШЕНИЯ

### 1.1 Что оставить, что выбросить, что переписать

**Решение: переписать с нуля на WPF + EF Core.**

Причины отказа от старого WinForms-проекта:

- WinForms — устаревшая технология, в дипломе её применение трудно обосновать как «новую разработку».
- Старая архитектура: никакого разделения логики и UI (всё в `Form2.cs`, `button1_Click` содержит бизнес-логику). Это критика на защите.
- `DataSource=KOSTYA\\SQLEXPRESS` — хардкод конкретной машины; приложение не запустится ни у кого другого.
- DinkToPdf требует нативной DLL (`libwkhtmltox`), которую сложно распространять.
- GitHub Copilot отлично знает WPF, MVVM, EF Core — код будет генерироваться чище и быстрее.

**Что сохранить (перенести) из старого кода:**

| Компонент | Действие |
|---|---|
| Логика `TicketGenerator.cs` (ExtractQuestions, GenerateTickets, ReplaceTextPlaceholders) | Перенести 1-в-1, это ядро системы |
| Класс `Ticket` с полями | Перенести, добавить новые поля |
| SQL-схема таблиц (Specialties, Subjects, Teachers, Groups) | Перенести и расширить |
| Шаблон `TicketTemplate.docx` | Использовать без изменений |
| Логика валидации файла вопросов | Перенести |

---

### 1.2 Стек технологий (оптимизирован под GitHub Copilot)

| Слой | Технология | Почему |
|---|---|---|
| UI | **WPF (.NET 8)** | MVVM, DataBinding, современный вид, хорошо известен Copilot |
| UI-стиль | **MaterialDesignThemes** (NuGet) | 1 пакет — Material Design готовый, хорошо документирован |
| MVVM | **CommunityToolkit.Mvvm** (NuGet) | Стандарт индустрии, Copilot знает идеально |
| ORM / БД | **Entity Framework Core 8 + SQL Server LocalDB** | LocalDB не требует установки сервера; EF Core генерирует миграции |
| Авторизация | **BCrypt.Net-Next** (NuGet) | Хеширование паролей, 1 строка кода |
| Word-документы | **DocumentFormat.OpenXml** (NuGet) | Уже использовался, переносится |
| PDF | **QuestPDF** (NuGet) | Проще чем DinkToPdf, не требует нативных DLL, MIT лицензия |
| Предпросмотр docx | **Microsoft Word Interop** или конвертация в PDF через QuestPDF и показ через `WebBrowser` | |
| Логирование | **Serilog** (NuGet) | Аудит действий пользователей |

---

## ЧАСТЬ 2 — ТЕХНИЧЕСКОЕ ЗАДАНИЕ

### 2.1 Наименование системы

**«Система автоматизированной генерации экзаменационных билетов»**
Версия 3.0. Дипломный проект.

### 2.2 Назначение

Автоматизация процесса создания, хранения и печати комплектов экзаменационных билетов для преподавателей СПО/ВО с разграничением прав доступа по ролям.

### 2.3 Роли пользователей и права доступа

#### Роль: Администратор
- Полный доступ ко всем функциям системы
- Управление пользователями: добавление/редактирование/блокировка учителей и председателей
- Управление справочниками: специальности, предметы, группы
- Просмотр журнала аудита
- Изменение названия образовательного учреждения
- Создание экзаменационных билетов

#### Роль: Председатель
- Редактирование полей `chairman`, `affirmer`, `afirmerlastname` в шаблоне
- Добавление новых преподавателей (без создания учётных записей)
- Добавление специальностей, предметов, групп в справочники
- Просмотр всех билетов, созданных по курируемым специальностям
- **Не может**: управлять учётными записями, видеть журнал аудита

#### Роль: Учитель
- Создание комплектов экзаменационных билетов по своим предметам
- Предпросмотр, сохранение (docx/pdf), печать билетов
- Просмотр истории своих билетов
- **Не может**: редактировать справочники, управлять пользователями

### 2.4 Функциональные требования

#### 2.4.1 Авторизация (новое)
- Экран входа с полями «Логин» и «Пароль»
- Хеширование паролей (BCrypt)
- Разные главные меню в зависимости от роли
- Кнопка «Выйти» на всех экранах

#### 2.4.2 Создание билетов (перенос + улучшение)
- Выбор предмета, специальности, группы из справочников (ComboBox, данные из БД)
- Ввод: номер протокола, дата протокола, тип экзамена, семестр, дата утверждения
- Поля председателя/утверждающего — автоподстановка из профиля Председателя или ручной ввод
- Выбор количества вопросов в билете: 1, 2, 3 или 4 (RadioButton или NumericUpDown)
- Выбор файла с вопросами с валидацией формата
- Расчёт и отображение максимального количества билетов
- Ввод нужного количества билетов (не более максимума)
- Генерация с случайным порядком вопросов без повторений
- Автоматическая нумерация билетов

#### 2.4.3 Предпросмотр и вывод (перенос + улучшение)
- Предпросмотр сгенерированного документа во встроенном средстве просмотра
- Сохранение в формате .docx
- Сохранение в формате .pdf
- Печать (PrintDialog)

#### 2.4.4 База данных — единая форма управления (новое)
- Одна форма «Управление справочниками» с панелью навигации слева
- При выборе раздела (Специальности / Предметы / Группы / Преподаватели) справа загружается соответствующий DataGrid
- Строки редактируются прямо в DataGrid (inline editing)
- Кнопки «Добавить строку» и «Удалить строку» (одни и те же для всех таблиц)
- Кнопка «Сохранить изменения» фиксирует всё в БД
- Доступ к разделам зависит от роли

#### 2.4.5 Управление пользователями (новое, только Admin)
- Список пользователей с фильтрацией по роли
- Добавление нового пользователя: логин, пароль, ФИО, роль
- Редактирование: смена ФИО, роли, блокировка/разблокировка
- Сброс пароля

#### 2.4.6 История и поиск (перенос + улучшение)
- Таблица созданных комплектов билетов
- Фильтр по предмету, специальности, дате, создателю
- Кнопки: открыть файл, повторно скачать, удалить запись

#### 2.4.7 Настройки (перенос + улучшение)
- Изменение названия образовательного учреждения (только Admin)
- Тема оформления (светлая / тёмная) — MaterialDesign
- Размер шрифта интерфейса

#### 2.4.8 Журнал аудита (новое, только Admin)
- Таблица: пользователь, действие, дата/время, детали
- Фильтрация по пользователю и типу действия
- Экспорт в xlsx или csv

#### 2.4.9 Справка
- Раздел «О программе» с версией
- Пример готового билета (изображение или PDF)
- Пример файла с вопросами с описанием формата маркеров

### 2.5 Нефункциональные требования

- **ОС**: Windows 10/11 (x64)
- **БД**: SQL Server LocalDB 2019+ (устанавливается вместе с Visual Studio)
- **Runtime**: .NET 8 Desktop Runtime
- **Разрешение**: от 1280×768
- **Установка**: Installer (опционально, ClickOnce или Inno Setup)

---

## ЧАСТЬ 3 — СХЕМА БАЗЫ ДАННЫХ

```sql
-- Пользователи системы
CREATE TABLE Users (
    UserID      INT PRIMARY KEY IDENTITY,
    Login       NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName    NVARCHAR(150) NOT NULL,
    Role        NVARCHAR(20) NOT NULL CHECK (Role IN ('Admin','Chairman','Teacher')),
    IsActive    BIT NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2 DEFAULT GETDATE()
);

-- Специальности
CREATE TABLE Specialties (
    SpecialtyID     INT PRIMARY KEY IDENTITY,
    SpecialtyName   NVARCHAR(100) NOT NULL,
    SpecialtyNumber NVARCHAR(50) NOT NULL
);

-- Предметы
CREATE TABLE Subjects (
    SubjectID   INT PRIMARY KEY IDENTITY,
    SubjectName NVARCHAR(100) NOT NULL,
    SpecialtyID INT NOT NULL FOREIGN KEY REFERENCES Specialties(SpecialtyID),
    Semester    INT NOT NULL
);

-- Группы
CREATE TABLE Groups (
    GroupID     INT PRIMARY KEY IDENTITY,
    SpecialtyID INT NOT NULL FOREIGN KEY REFERENCES Specialties(SpecialtyID),
    GroupNumber NVARCHAR(50) NOT NULL
);

-- Преподаватели (справочник ФИО, отдельно от учётных записей)
CREATE TABLE Teachers (
    TeacherID   INT PRIMARY KEY IDENTITY,
    FullName    NVARCHAR(150) NOT NULL,
    UserID      INT NULL FOREIGN KEY REFERENCES Users(UserID) -- null если нет уч. записи
);

-- Привязка учителей к предметам (многие-ко-многим)
CREATE TABLE TeacherSubjects (
    TeacherID INT NOT NULL FOREIGN KEY REFERENCES Teachers(TeacherID),
    SubjectID INT NOT NULL FOREIGN KEY REFERENCES Subjects(SubjectID),
    PRIMARY KEY (TeacherID, SubjectID)
);

-- Файлы вопросов
CREATE TABLE QuestionDocuments (
    QuestionID  INT PRIMARY KEY IDENTITY,
    FilePath    NVARCHAR(500) NOT NULL,
    SubjectID   INT FOREIGN KEY REFERENCES Subjects(SubjectID),
    UploadedBy  INT FOREIGN KEY REFERENCES Users(UserID),
    UploadedAt  DATETIME2 DEFAULT GETDATE()
);

-- Файлы билетов
CREATE TABLE TicketDocuments (
    TicketID      INT PRIMARY KEY IDENTITY,
    FilePathDocx  NVARCHAR(500),
    FilePathPdf   NVARCHAR(500),
    GeneratedBy   INT FOREIGN KEY REFERENCES Users(UserID),
    GeneratedAt   DATETIME2 DEFAULT GETDATE()
);

-- События экзаменов (главная рабочая таблица)
CREATE TABLE ExamEvents (
    EventID          INT PRIMARY KEY IDENTITY,
    SubjectID        INT NOT NULL FOREIGN KEY REFERENCES Subjects(SubjectID),
    SpecialtyID      INT NOT NULL FOREIGN KEY REFERENCES Specialties(SpecialtyID),
    GroupID          INT NOT NULL FOREIGN KEY REFERENCES Groups(GroupID),
    TeacherID        INT NOT NULL FOREIGN KEY REFERENCES Teachers(TeacherID),
    QuestionID       INT FOREIGN KEY REFERENCES QuestionDocuments(QuestionID),
    TicketID         INT FOREIGN KEY REFERENCES TicketDocuments(TicketID),
    ExamDate         DATE NOT NULL,
    ProtocolNumber   NVARCHAR(20),
    CommissionName   NVARCHAR(200),
    Chairman         NVARCHAR(150),
    Affirmer         NVARCHAR(100),
    AffirmerLastName NVARCHAR(100),
    DateOfStatement  DATE,
    Semester         INT,
    ExamType         NVARCHAR(100),
    TicketCount      INT,
    QuestionsPerTicket INT DEFAULT 3,
    CreatedBy        INT FOREIGN KEY REFERENCES Users(UserID),
    CreatedAt        DATETIME2 DEFAULT GETDATE()
);

-- Настройки системы
CREATE TABLE Settings (
    SettingKey   NVARCHAR(100) PRIMARY KEY,
    SettingValue NVARCHAR(1000)
);

-- Журнал аудита
CREATE TABLE AuditLog (
    LogID     INT PRIMARY KEY IDENTITY,
    UserID    INT FOREIGN KEY REFERENCES Users(UserID),
    Action    NVARCHAR(100) NOT NULL,
    Details   NVARCHAR(1000),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Начальные данные
INSERT INTO Settings VALUES ('InstitutionName', 'Название образовательного учреждения');
INSERT INTO Users (Login, PasswordHash, FullName, Role) 
VALUES ('admin', '<bcrypt_hash_of_admin>', 'Администратор', 'Admin');
```

---

## ЧАСТЬ 4 — АРХИТЕКТУРА ПРОЕКТА

### 4.1 Структура решения (Solution)

```
ExamTickets.sln
├── ExamTickets.Core/               # Бизнес-логика (Class Library)
│   ├── Models/                     # Сущности БД (EF Core)
│   │   ├── User.cs
│   │   ├── Specialty.cs
│   │   ├── Subject.cs
│   │   ├── Group.cs
│   │   ├── Teacher.cs
│   │   ├── ExamEvent.cs
│   │   ├── QuestionDocument.cs
│   │   ├── TicketDocument.cs
│   │   ├── AuditLog.cs
│   │   └── Setting.cs
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core DbContext
│   ├── Services/
│   │   ├── AuthService.cs          # Авторизация, хеширование
│   │   ├── TicketGeneratorService.cs # Логика генерации (из старого кода)
│   │   ├── DocumentService.cs      # Работа с docx/pdf
│   │   ├── DatabaseService.cs      # CRUD для справочников
│   │   ├── AuditService.cs         # Запись в журнал аудита
│   │   └── SettingsService.cs      # Чтение/запись настроек
│   └── DTOs/
│       └── TicketFormData.cs       # Данные формы создания билетов
│
└── ExamTickets.WPF/                # WPF-приложение
    ├── App.xaml / App.xaml.cs
    ├── Views/
    │   ├── LoginWindow.xaml
    │   ├── MainWindow.xaml         # Shell с навигационной панелью
    │   ├── Pages/
    │   │   ├── DashboardPage.xaml
    │   │   ├── TicketCreationPage.xaml
    │   │   ├── DatabaseManagementPage.xaml
    │   │   ├── UserManagementPage.xaml
    │   │   ├── HistoryPage.xaml
    │   │   ├── SettingsPage.xaml
    │   │   ├── AuditLogPage.xaml
    │   │   └── HelpPage.xaml
    │   └── Controls/
    │       ├── TicketPreviewControl.xaml
    │       └── RoleBasedMenu.xaml
    ├── ViewModels/
    │   ├── LoginViewModel.cs
    │   ├── MainViewModel.cs
    │   ├── DashboardViewModel.cs
    │   ├── TicketCreationViewModel.cs
    │   ├── DatabaseManagementViewModel.cs
    │   ├── UserManagementViewModel.cs
    │   ├── HistoryViewModel.cs
    │   ├── SettingsViewModel.cs
    │   └── AuditLogViewModel.cs
    ├── Converters/                  # IValueConverter для биндинга
    ├── Resources/
    │   ├── Styles.xaml
    │   ├── Colors.xaml
    │   └── TicketTemplate.docx     # Встроенный шаблон
    └── Helpers/
        └── NavigationService.cs
```

### 4.2 Паттерн MVVM (краткое объяснение для Copilot-промптов)

- **Model** = классы в `ExamTickets.Core/Models/` (EF Core сущности)
- **ViewModel** = классы в `ViewModels/`, наследуют `ObservableObject` из CommunityToolkit.Mvvm
- **View** = XAML-файлы, биндятся к ViewModel через `DataContext`
- Никакой бизнес-логики в code-behind (.xaml.cs) — только биндинг

---

## ЧАСТЬ 5 — ПЛАН РЕАЛИЗАЦИИ (для GitHub Copilot)

Это пошаговый план. Каждый шаг — отдельный промпт для Copilot.

---

### ШАГ 0 — Подготовка проекта (делать вручную)

```
1. Создать Solution в Visual Studio 2022:
   - Проект ExamTickets.Core (Class Library, .NET 8)
   - Проект ExamTickets.WPF (WPF App, .NET 8)
   
2. NuGet пакеты для ExamTickets.Core:
   - Microsoft.EntityFrameworkCore.SqlServer
   - Microsoft.EntityFrameworkCore.Tools
   - BCrypt.Net-Next
   - DocumentFormat.OpenXml
   - QuestPDF
   - Serilog.Sinks.File

3. NuGet пакеты для ExamTickets.WPF:
   - MaterialDesignThemes
   - CommunityToolkit.Mvvm
   - Microsoft.Extensions.DependencyInjection
   - (ссылка на ExamTickets.Core)

4. Подключить LocalDB строку:
   "Server=(localdb)\\mssqllocaldb;Database=ExamTicketsDB;Trusted_Connection=True;"
```

---

### ШАГ 1 — Промпт для Copilot: Модели и DbContext

```
Задача: создать модели Entity Framework Core и DbContext для приложения генерации экзаменационных билетов.

Создай следующие классы в папке Models:
- User.cs: UserID (int PK), Login (string, уникальный), PasswordHash (string), 
  FullName (string), Role (enum: Admin/Chairman/Teacher), IsActive (bool), CreatedAt (DateTime)
- Specialty.cs: SpecialtyID (int PK), SpecialtyName (string), SpecialtyNumber (string)
- Subject.cs: SubjectID (int PK), SubjectName (string), SpecialtyID (FK), Semester (int)
- Group.cs: GroupID (int PK), SpecialtyID (FK), GroupNumber (string)
- Teacher.cs: TeacherID (int PK), FullName (string), UserID (int? FK nullable)
  + навигационное свойство ICollection<Subject> Subjects через TeacherSubjects
- ExamEvent.cs: EventID (int PK), SubjectID FK, SpecialtyID FK, GroupID FK, TeacherID FK,
  QuestionID FK nullable, TicketID FK nullable, ExamDate, ProtocolNumber, CommissionName,
  Chairman, Affirmer, AffirmerLastName, DateOfStatement, Semester, ExamType, 
  TicketCount, QuestionsPerTicket (default 3), CreatedBy FK, CreatedAt
- QuestionDocument.cs: QuestionID (int PK), FilePath, SubjectID FK nullable, UploadedBy FK, UploadedAt
- TicketDocument.cs: TicketID (int PK), FilePathDocx, FilePathPdf, GeneratedBy FK, GeneratedAt
- Setting.cs: SettingKey (string PK), SettingValue (string)
- AuditLog.cs: LogID (int PK), UserID FK, Action (string), Details (string), CreatedAt

Создай AppDbContext.cs с DbSet для каждой модели.
В OnModelCreating настрой:
- Индексы на Login в Users
- Составной PK для TeacherSubjects (TeacherID + SubjectID)
- Cascade delete: при удалении Specialty удалять связанные Subjects и Groups
- HasData seed: один пользователь Admin с логином "admin", хешированный BCrypt пароль "admin123"
- HasData seed: одна запись в Settings с ключом "InstitutionName"

Строка подключения должна читаться из appsettings.json.

Язык: C#, .NET 8, EF Core 8.
```

---

### ШАГ 2 — Промпт: AuthService

```
Создай класс AuthService в папке Services.

Зависимости: AppDbContext (через DI), BCrypt.Net.

Методы:
1. Task<User?> LoginAsync(string login, string password)
   - Ищет пользователя по логину в БД
   - Проверяет BCrypt.Verify(password, user.PasswordHash)
   - Если IsActive == false — возвращает null
   - Возвращает User или null

2. Task<bool> CreateUserAsync(string login, string password, string fullName, UserRole role)
   - Проверяет что логин не занят
   - Хеширует пароль BCrypt.HashPassword(password, 12)
   - Создаёт и сохраняет нового User

3. Task<bool> ChangePasswordAsync(int userId, string newPassword)

4. Task<bool> ToggleUserActiveAsync(int userId)

Также создай статический класс CurrentUser со свойствами:
- User? Instance
- bool IsAdmin → Instance?.Role == UserRole.Admin
- bool IsChairman → Instance?.Role == UserRole.Chairman  
- bool IsTeacher → Instance?.Role == UserRole.Teacher
- void SetUser(User user)
- void Clear()

Язык: C#, async/await, EF Core.
```

---

### ШАГ 3 — Промпт: TicketGeneratorService

```
Перенеси и адаптируй класс TicketGenerator из WinForms-проекта в TicketGeneratorService.cs.

Исходные методы (перенести без изменений):
- ExtractQuestions(string filePath, List<string> markers) → Dictionary<string, List<string>>
- GenerateTickets(Dictionary<string, List<string>> questions, List<string> markers, int count) → List<List<string>>
- ValidateQuestionFile(string filePath, List<string> markers) → bool (убрать MessageBox, пробрасывать исключения)
- CalculateMaxTickets(Dictionary<string, List<string>> questions, List<string> markers) → int
- ReplaceTextPlaceholders(OpenXmlElement root, Dictionary<string, string> replacements) → void
- IsFileLocked(string path) → bool

Метод FillTemplate адаптировать под новый класс TicketFormData:
public class TicketFormData {
    public string EducationalInstitution { get; set; }
    public string Commission { get; set; }
    public string ProtocolNumber { get; set; }
    public string Date { get; set; }
    public string Chairman { get; set; }
    public string SpecialtyNumber { get; set; }
    public string ExamType { get; set; }
    public string Exam { get; set; }
    public string GroupsNumber { get; set; }
    public string Semester { get; set; }
    public string Affirmer { get; set; }
    public string AffirmerLastName { get; set; }
    public string DateOfStatement { get; set; }
    public string Teachers { get; set; }
    public int TicketCount { get; set; }
    public int QuestionsPerTicket { get; set; }  // 1-4
    public string QuestionFilePath { get; set; }
    public string TemplatePath { get; set; }
}

Новый публичный метод:
Task<byte[]> GenerateTicketsDocumentAsync(TicketFormData data)
- Читает шаблон из data.TemplatePath (или встроенного ресурса)
- Заполняет все placeholder'ы из data
- Генерирует билеты
- Возвращает byte[] готового docx

Язык: C#, DocumentFormat.OpenXml, async.
```

---

### ШАГ 4 — Промпт: DocumentService (PDF и печать)

```
Создай DocumentService.cs в папке Services.

Зависимости: QuestPDF, System.Printing.

Методы:

1. Task SaveAsDocxAsync(byte[] documentBytes, string filePath)
   - Сохраняет byte[] в файл .docx

2. Task SaveAsPdfAsync(byte[] docxBytes, string filePath)
   - Конвертирует docx в pdf через LibreOffice (soffice --headless --convert-to pdf)
   - Если LibreOffice не установлен — выбросить понятное исключение с инструкцией

3. Task PrintDocumentAsync(byte[] docxBytes)
   - Сохраняет во временный файл
   - Открывает PrintDialog
   - Печатает через Process.Start с глаголом "print"

4. string GetTemplatePath()
   - Возвращает путь к встроенному TicketTemplate.docx (из папки Resources рядом с exe)

Язык: C#, async.
```

---

### ШАГ 5 — Промпт: LoginWindow (View + ViewModel)

```
Создай экран входа в систему для WPF-приложения.

LoginViewModel.cs (наследует ObservableObject из CommunityToolkit.Mvvm):
Свойства:
- [ObservableProperty] string login
- [ObservableProperty] string password  
- [ObservableProperty] string errorMessage
- [ObservableProperty] bool isLoading
Команды:
- [RelayCommand] async Task LoginAsync()
  * Устанавливает IsLoading = true
  * Вызывает AuthService.LoginAsync(Login, Password)
  * Если успех: CurrentUser.SetUser(result), открывает MainWindow, закрывает LoginWindow
  * Если неудача: ErrorMessage = "Неверный логин или пароль"
  * IsLoading = false

LoginWindow.xaml:
- Центрированная форма в стиле Material Design
- Логотип/иконка сверху
- TextBox для логина
- PasswordBox для пароля (привязать через attached property или code-behind)
- Кнопка «Войти» (IsEnabled привязан к !IsLoading)
- TextBlock для ошибки (красный, видим только при непустом ErrorMessage)
- ProgressBar или Spinner при IsLoading

Использовать MaterialDesignThemes для стилизации.
Язык: C#, WPF, MVVM, CommunityToolkit.Mvvm.
```

---

### ШАГ 6 — Промпт: MainWindow с навигацией

```
Создай главное окно приложения с навигационной боковой панелью.

MainWindow.xaml:
- Разделить на две колонки: узкая левая (навигация) и широкая правая (контент Frame)
- Левая панель: кнопки навигации с иконками MaterialDesign и подписями:
  * Дашборд (только Admin/Chairman)
  * Создание билетов
  * Справочники (скрыта для Teacher)
  * Пользователи (только Admin)
  * История билетов
  * Журнал аудита (только Admin)
  * Настройки
  * Справка
  * [внизу] Текущий пользователь (ФИО + роль)
  * [внизу] Кнопка «Выйти»

MainViewModel.cs:
- Свойство CurrentPage (Page)
- Команды NavigateTo* для каждой страницы
- При навигации проверять права роли (если нет доступа — игнорировать)
- Команда LogoutCommand: CurrentUser.Clear(), открыть LoginWindow, закрыть MainWindow

Видимость пунктов меню управляется через Visibility-конвертер + CurrentUser.IsAdmin и т.д.

Язык: C#, WPF, MVVM.
```

---

### ШАГ 7 — Промпт: TicketCreationPage

```
Создай страницу создания экзаменационных билетов.

TicketCreationViewModel.cs:
Свойства (все ObservableProperty):
- ObservableCollection<Specialty> Specialties
- ObservableCollection<Subject> Subjects (фильтруется по выбранной специальности)
- ObservableCollection<Group> Groups (фильтруется по выбранной специальности)
- ObservableCollection<Teacher> Teachers
- Specialty? SelectedSpecialty (при изменении перегружать Subjects и Groups)
- Subject? SelectedSubject
- Group? SelectedGroup
- Teacher? SelectedTeacher
- string ProtocolNumber, Date, ExamType, Semester, Chairman, Affirmer, AffirmerLastName, DateOfStatement
- int QuestionsPerTicket (1-4, default 3)
- string QuestionFilePath
- int MaxTickets, TicketCount
- bool CanGenerate (все поля заполнены и файл выбран)
- byte[]? GeneratedDocumentBytes
- bool IsGenerated

Команды:
- LoadDataCommand (async): загружает Specialties, Teachers из БД
- BrowseQuestionFileCommand: OpenFileDialog для .docx, затем ValidateQuestionFile
- GenerateCommand (async): вызывает TicketGeneratorService.GenerateTicketsDocumentAsync
- PreviewCommand: показывает PreviewWindow с документом
- SaveDocxCommand: диалог сохранения, DocumentService.SaveAsDocxAsync, сохраняет в БД
- SavePdfCommand: диалог сохранения, DocumentService.SaveAsPdfAsync, сохраняет в БД
- PrintCommand: DocumentService.PrintDocumentAsync

TicketCreationPage.xaml:
Разделить на горизонтальные секции (GroupBox или Card от MaterialDesign):
1. «Общие данные»: ComboBox специальности, предмета, группы, учителя
2. «Реквизиты протокола»: TextBox номер, DatePicker дата, TextBox комиссия
3. «Реквизиты утверждения»: TextBox Chairman, Affirmer, AffirmerLastName, DatePicker
4. «Параметры билета»: RadioButton или Slider 1-4 вопроса, NumericUpDown количество билетов
5. «Файл вопросов»: путь + кнопка Обзор + индикатор валидации
6. Кнопки: «Создать билеты», «Предпросмотр», «Сохранить docx», «Сохранить PDF», «Печать»
   (Предпросмотр, Сохранить, Печать — активны только после генерации)

Язык: C#, WPF, MVVM, MaterialDesignThemes.
```

---

### ШАГ 8 — Промпт: DatabaseManagementPage (единая форма)

```
Создай страницу управления справочниками — ОДНУ форму для всех таблиц.

DatabaseManagementViewModel.cs:
- ObservableCollection<string> TableNames = ["Специальности", "Предметы", "Группы", "Преподаватели"]
- string SelectedTable (при смене — LoadTableData)
- DataTable? CurrentTableData (для DataGrid с динамическими колонками)
- Команды:
  * LoadTableDataCommand (async): загружает данные выбранной таблицы
  * AddRowCommand: добавляет пустую строку в DataTable
  * DeleteRowCommand(DataRowView row): удаляет строку (с подтверждением)
  * SaveChangesCommand (async): определяет изменённые/добавленные/удалённые строки и сохраняет в БД через EF Core
  * RefreshCommand

DatabaseManagementPage.xaml:
- Слева: ListBox с TableNames (NavigationRailItem из MaterialDesign)
- Справа вверху: заголовок выбранной таблицы + кнопки «Добавить строку», «Удалить», «Сохранить», «Обновить»
- Справа: DataGrid с AutoGenerateColumns=True, CanUserAddRows=False, CanUserDeleteRows=False
  (управление строками только через кнопки)
- Строки со статусом Added выделить зелёным, Modified — жёлтым, Deleted — перечеркнуть

Видимость кнопки «Сохранить» и всей страницы — только для Admin и Chairman.

Язык: C#, WPF, MVVM.
```

---

### ШАГ 9 — Промпт: UserManagementPage (только Admin)

```
Создай страницу управления пользователями (только для роли Admin).

UserManagementViewModel.cs:
- ObservableCollection<User> Users
- User? SelectedUser
- Фильтр по роли (All/Admin/Chairman/Teacher)
- Форма добавления/редактирования (в той же странице, справа или в диалоге):
  * Login, FullName, Role (ComboBox), IsActive (Toggle)
- Команды:
  * LoadUsersCommand
  * AddUserCommand → открывает диалог, создаёт пользователя
  * EditUserCommand → заполняет форму выбранным пользователем
  * SaveUserCommand → CreateUserAsync или обновление
  * ToggleActiveCommand → включить/выключить пользователя
  * ResetPasswordCommand → диалог нового пароля

UserManagementPage.xaml:
- Верхняя панель: фильтры ролей (ToggleButton) + кнопка «Добавить пользователя»
- DataGrid со столбцами: Логин, ФИО, Роль, Статус (активен/заблокирован), Дата создания
- Каждая строка: кнопки «Редактировать», «Сброс пароля», «Блокировать/Разблокировать»
- Диалог добавления/редактирования: MaterialDesign DialogHost

Язык: C#, WPF, MVVM.
```

---

### ШАГ 10 — Промпт: HistoryPage

```
Создай страницу истории созданных билетов.

HistoryViewModel.cs:
- ObservableCollection<ExamEvent> Events
- Фильтры: SelectedSpecialty (nullable), SubjectName (string поиск), DateFrom, DateTo
  (для Teacher: Events только с CreatedBy == CurrentUser.Instance.UserID)
- Команды:
  * LoadEventsCommand (async, с применением фильтров)
  * OpenDocxCommand(ExamEvent e): Process.Start(e.TicketDocument.FilePathDocx)
  * OpenPdfCommand(ExamEvent e): Process.Start(e.TicketDocument.FilePathPdf)
  * DeleteEventCommand(ExamEvent e): с подтверждением, удаляет запись из БД

HistoryPage.xaml:
- Панель фильтров сверху: ComboBox специальности, TextBox предмет, DatePicker от/до, кнопка «Найти»
- DataGrid: Предмет, Специальность, Группа, Учитель, Дата, Кол-во билетов, Вопросов, Создатель
- Для каждой строки: кнопки «Открыть docx», «Открыть PDF» (если файл существует), «Удалить»

Язык: C#, WPF, MVVM.
```

---

### ШАГ 11 — Промпт: Настройки и аудит

```
1. SettingsPage:
SettingsViewModel:
- string InstitutionName (загружается из Settings в БД, SettingKey="InstitutionName")
- Тема оформления: Light/Dark (MaterialDesignTheme.Modify)
- Команда SaveSettingsCommand (сохраняет InstitutionName в БД)

Поле InstitutionName — только для Admin (для других — ReadOnly TextBox).

2. AuditLogPage (только Admin):
AuditLogViewModel:
- ObservableCollection<AuditLog> Logs
- Фильтры: пользователь, тип действия, период
- ExportToCsvCommand: сохраняет в CSV через StreamWriter

AuditLogPage.xaml:
- DataGrid: Пользователь, Действие, Детали, Дата/время
- Кнопки: «Обновить», «Экспорт CSV»

Язык: C#, WPF, MVVM.
```

---

### ШАГ 12 — Промпт: AuditService (интеграция)

```
Создай AuditService.cs.

Метод: Task LogAsync(string action, string details = "")
- Создаёт запись AuditLog с CurrentUser.Instance.UserID, action, details, DateTime.Now
- Сохраняет в БД асинхронно

Добавь вызовы AuditService.LogAsync в ключевые места:
- В AuthService.LoginAsync при успешном входе: Log("Вход в систему")
- В TicketCreationViewModel.SaveDocxCommand: Log("Создан комплект билетов", $"Предмет: {subject}, Кол-во: {count}")
- В DatabaseManagementViewModel.SaveChangesCommand: Log("Изменён справочник", $"Таблица: {tableName}")
- В UserManagementViewModel при создании/блокировке пользователя: соответствующие сообщения

Язык: C#, EF Core, async.
```

---

### ШАГ 13 — Промпт: App.xaml.cs — DI-контейнер и запуск

```
Настрой Dependency Injection в App.xaml.cs.

В OnStartup:
- Создать ServiceCollection
- Добавить AppDbContext с строкой подключения из appsettings.json
- Зарегистрировать как Scoped/Transient: AuthService, TicketGeneratorService, 
  DocumentService, DatabaseService, AuditService, SettingsService
- Зарегистрировать все ViewModel как Transient
- Зарегистрировать LoginWindow, MainWindow как Transient
- Построить ServiceProvider
- Применить миграции: context.Database.MigrateAsync()
- Показать LoginWindow

Создай appsettings.json:
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=ExamTicketsDB;Trusted_Connection=True;"
  }
}

Язык: C#, Microsoft.Extensions.DependencyInjection, EF Core Migrations.
```

---

### ШАГ 14 — Финальные команды EF Core (выполнять в Package Manager Console)

```
Add-Migration InitialCreate -Project ExamTickets.Core -StartupProject ExamTickets.WPF
Update-Database -Project ExamTickets.Core -StartupProject ExamTickets.WPF
```

---

## ЧАСТЬ 6 — НОВЫЕ ФУНКЦИИ ДЛЯ ДИПЛОМА (отличия от курсовой)

Это важно описать в пояснительной записке как «доработки»:

| Функция | Курсовой | Дипломный |
|---|---|---|
| Авторизация | Нет | Вход по логину/паролю, хеширование BCrypt |
| Разграничение ролей | Нет | Admin / Председатель / Учитель |
| Интерфейс | WinForms | WPF (MVVM, Material Design) |
| Форм для БД | Несколько отдельных | Одна универсальная с динамическими колонками |
| Управление пользователями | Нет | Полная CRUD Admin-панель |
| Журнал аудита | Нет | Вся активность пользователей логируется |
| Дашборд | Нет | Статистика: билетов создано, предметов, пользователей |
| Количество вопросов | 1-3 | 1-4 |
| Хранение файлов | Пути в БД | Пути + метаданные (кто, когда) |
| Тема | Фиксированная | Light/Dark переключение |
| ORM | ADO.NET вручную | Entity Framework Core 8 с миграциями |
| БД | SQL Server Express (хардкод) | LocalDB (не требует установки сервера) |
| PDF | DinkToPdf (нативная DLL) | QuestPDF (managed) |

---

## ЧАСТЬ 7 — СОВЕТЫ ПО ВАЙБКОДИНГУ С COPILOT

1. **Начинай каждый промпт с контекста**: «В проекте используется WPF, .NET 8, EF Core 8, CommunityToolkit.Mvvm, MaterialDesignThemes. Паттерн MVVM.»

2. **Давай Copilot полный класс для редактирования**, не фрагмент — так он лучше понимает контекст.

3. **Последовательность**: сначала Models → DbContext → Services → ViewModels → Views. Не прыгай.

4. **Для DataGrid с динамическими колонками** уточняй: «используй DataTable и AutoGenerateColumns=True».

5. **Если Copilot генерирует MessageBox** — замени на DialogHost от MaterialDesign или на свойство ErrorMessage в ViewModel.

6. **После каждого шага** делай `Add-Migration` и проверяй, что БД обновляется без ошибок.

7. **Для PasswordBox** в WPF нет прямого биндинга — используй attached property или передавай пароль через метод в code-behind (это единственное исключение для MVVM в WPF).
```

---

*Документ составлен на основе анализа курсового проекта (WinForms, C#, SQL Server, OpenXML) и требований дипломной работы.*
