namespace Levity.Composition
{
    /// <summary>A module owned by a Composition lifecycle.</summary>
    public interface ICompositionModule
    {
        void Initialize(ICompositionServices services);
        void Start();
        void Shutdown();
    }
}
