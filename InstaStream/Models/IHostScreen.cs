namespace InstaStream.Models
{
    public interface IHostScreen
    {
        void ShowPage(AbstractPage page);
        
        bool IsLoading { get; set; }
    }
}