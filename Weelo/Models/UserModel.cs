using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Weelo.Models
{
    public class UserModel
    {
       
        [Required(ErrorMessage = "El {0} es obligatorio")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "El {0} es obligatorio")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

}
