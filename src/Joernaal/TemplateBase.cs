using System.Text;
using System.Threading.Tasks;

namespace Joernaal
{
    public abstract class TemplateBase
    {
        StringBuilder builder = new StringBuilder();
        string _body;

        public string Body { get => _body; set => _body = value; }

        public virtual void Write(object value)
        {
            // Escape HTML?
            builder.Append(value);
        }

        public virtual void WriteLiteral(object value)
        {
            builder.Append(value);
        }

        public virtual Task ExecuteAsync()
        {
            return Task.CompletedTask;
        }

        public string RenderBody()
        {
            return Body;
        }

        public string Source { get { return builder.ToString(); } }
    }
}