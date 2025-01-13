using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Practical.GenericDbContextEFCore.Common
{
    public class TempTableData
    {
        [Key]
        public Guid Id { get; set; }
    }

    public class TempTableDataString
    {
        [Key]
        public string Id { get; set; }
    }

    [Keyless]
    public class TempTableDataForGuidIdAndIntValue
    {
        public Guid GuidId { get; set; }
        public int IntValue { get; set; }
    }

    public class TempTableDataIntValue
    {
        [Key]
        public int Id { get; set; }
    }
}
