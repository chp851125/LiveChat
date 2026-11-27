using System.ComponentModel.DataAnnotations;
using LiveChat.Models;

namespace LiveChat.ViewModels
{
    public class CreateChannelViewModel
    {
        [Display(Name = "頻道名稱")]
        [Required(ErrorMessage = "請輸入頻道名稱。")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "類型")]
        public ChannelType Type { get; set; } = ChannelType.Public;
    }
}
