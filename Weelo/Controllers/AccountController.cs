using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Weelo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Weelo.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private string generatedToken = null;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IConfiguration config, ITokenService tokenService, IUserRepository userRepository, ILogger<AccountController> logger)
        {
            _config = config;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _logger = logger;
        }


        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [AllowAnonymous]
        //[Route("login")]
        [HttpPost]
        public IActionResult Login(UserModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {

                IActionResult response = Unauthorized();
                var validUser = GetUser(model);

                if (validUser != null)
                {
                    generatedToken = _tokenService.BuildToken(_config["Jwt:Key"].ToString(), _config["Jwt:Issuer"].ToString(),
                    validUser);

                    if (generatedToken != null)
                    {
                         HttpContext.Session.SetString("Token", generatedToken);
                        _logger.LogInformation(1, "Usuario Logeado.");
                        if(!string.IsNullOrEmpty(returnUrl))
                        {
                            return LocalRedirect(returnUrl);
                        }
                        return RedirectToAction("Index","Home");
                    }
                    else
                    {
                        //ViewBag.Message = "";
                        _logger.LogInformation(2, "No se pudo generar el Token.");
                        ModelState.AddModelError(string.Empty, "No se pudo generar el Token.");
                        return View(model);
                        //return (RedirectToAction("Error"));
                    }
                }
                else
                {
                    _logger.LogInformation(3, "Error de Autenticación.");
                    ModelState.AddModelError(string.Empty, "Error de Autenticación.");
                    return View(model);
                }
             
            }

            // If we got this far, something failed, redisplay form
            return View(model);

        }
        private UserDTO GetUser(UserModel userModel)
        {
            //Write your code here to authenticate the user
            return _userRepository.GetUser(userModel);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LogOff(string returnUrl = null)
        {
            HttpContext.Session.Clear();
            return LocalRedirect("/Account/Login");
        }

    }
}
