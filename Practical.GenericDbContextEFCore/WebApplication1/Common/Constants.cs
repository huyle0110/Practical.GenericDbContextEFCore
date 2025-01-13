namespace Practical.GenericDbContextEFCore.Common
{
    public static class CommonConstants
    {
        public const int CommandTimeout = 60;
        public const int SqlBulkCopyBatchSize = 3000;
        public const int MinimumIdsForCreateTempTable = 300;
        public const string ParameterProcedureAtSign = "@";
        public const int DbBulkUpdateTimeout = 300;
        public const string TextComma = ",";
        public const string ParameterProcedureOutput = "OUTPUT";
        public const int LimitDataReader = 5000;
        public const string HyphenSymbol = "-";
        public const string ReplaceEmpty = "";
        public const string CommasAndSpace = ", ";
        public const int MinimumRowForApplyTempTableInsert = 10;

    }
}
