using Caliburn.Micro;

namespace InstaStream.Models
{
    public abstract class AbstractPage : Screen
    {
        protected IHostScreen parent { get; }
        protected InstagramSession session { get; }
        
        public AbstractPage(IHostScreen parent, InstagramSession session)
        {
            this.parent = parent;
            this.session = session;
        }
    }
}