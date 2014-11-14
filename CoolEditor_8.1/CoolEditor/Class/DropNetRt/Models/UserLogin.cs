using System.Runtime.Serialization;

namespace CoolEditor.Class.DropNetRt.Models
{
    [DataContract]
    public class UserLogin
    {
        public string Token { get; set; }
        public string Secret { get; set; }
    }
}
