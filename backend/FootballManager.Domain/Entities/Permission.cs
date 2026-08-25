using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Permission : Entity
    {
        public string Code { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;
        public string Module { get; private set; } = string.Empty;

        protected Permission() { }

        public Permission(string code, string name, string module)
        {
            Code = !string.IsNullOrWhiteSpace(code) ? code : throw new ArgumentException("Code required.", nameof(code));
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name required.", nameof(name));
            Module = !string.IsNullOrWhiteSpace(module) ? module : throw new ArgumentException("Module required.", nameof(module));
        }
    }
}
