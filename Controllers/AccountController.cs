using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using cmse.Dtos;
using cmse.Repositories;
using cmse.Repositories.Models;
using cmse.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace cmse.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsuarioRepository _usuarioRepository;
        private readonly PasswordService _passwordService;

    public AccountController(
        UsuarioRepository usuarioRepository,
        PasswordService passwordService)
        {
            _usuarioRepository = usuarioRepository;
            _passwordService = passwordService;
        }

        public async Task<IActionResult> Register()
        {
            ViewBag.TiposDocumento = await _usuarioRepository.ObtenerTiposDocumentoAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegistroDto modelo)
        {
            ViewBag.TiposDocumento = await _usuarioRepository.ObtenerTiposDocumentoAsync();

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            if (await _usuarioRepository.ExisteCorreoAsync(modelo.Correo))
            {
                ModelState.AddModelError("Correo", "El correo ya está registrado.");
                return View(modelo);
            }

            if (await _usuarioRepository.ExisteNumeroDocumentoAsync(modelo.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "El número de documento ya está registrado.");
                return View(modelo);
            }

            var usuario = new Usuario
            {
                IdRol = 3,
                IdTipoDocumento = modelo.IdTipoDocumento,
                Nombres = modelo.Nombres,
                Apellidos = modelo.Apellidos,
                NumeroDocumento = modelo.NumeroDocumento,
                Correo = modelo.Correo,
                Telefono = modelo.Telefono,
                Contrasena = _passwordService.HashPassword(modelo.Contrasena),
                Estado = true,
                IntentosFallidos = 0
            };

            await _usuarioRepository.CrearAsync(usuario);

            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario = await _usuarioRepository.ObtenerPorCorreoAsync(modelo.Correo);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(modelo);
            }

            if (!_passwordService.VerifyPassword(modelo.Contrasena, usuario.Contrasena))
            {
                ModelState.AddModelError("", "Correo o contraseña incorrectos.");
                return View(modelo);
            }

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombres),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Role, usuario.Rol?.Nombre ?? "")
        };

            var identidad = new ClaimsIdentity(claims, "CMSECookie");

            var propiedades = new AuthenticationProperties
            {
                IsPersistent = modelo.Recordarme
            };

            await HttpContext.SignInAsync(
                "CMSECookie",
                new ClaimsPrincipal(identidad),
                propiedades);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CMSECookie");

            return RedirectToAction("Index", "Home");
        }
    }

}
