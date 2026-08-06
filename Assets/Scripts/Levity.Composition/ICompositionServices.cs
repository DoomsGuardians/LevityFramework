namespace Levity.Composition
{
    /// <summary>Provides explicitly registered dependencies to modules during initialization.</summary>
    public interface ICompositionServices
    {
        TService Get<TService>() where TService : class;
    }
}
