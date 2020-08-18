using EPiServer.Core;

namespace AlloyWeb.Models.Pages
{
    public interface IHasRelatedContent
    {
        ContentArea RelatedContentArea { get; }
    }
}
