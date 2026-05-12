using System;
using System.Data;

namespace ExamTickets.WPF.Extensions
{
    internal static class DataRowExtensions
    {
        // Безопасный вызов IsNull: защищаемся от null-колонки, чтобы убрать предупреждение CS8604
        public static bool SafeIsNull(this DataRow row, DataColumn? column)
        {
            if (row is null) throw new ArgumentNullException(nameof(row));
            if (column is null) return true; // если колонка отсутствует — считаем значение как null
            return row.IsNull(column);
        }
    }
}