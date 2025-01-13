namespace Practical.GenericDbContextEFCore.Common
{
    public interface IGuidKeyEntity
    {
        public Guid Id { get; set; }
    }

    public interface IIntKeyEntity
    {
        public int Id { get; set; }
    }
}
