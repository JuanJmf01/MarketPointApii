﻿using AutoMapper;
using MarketPointApi.DTOs;
using MarketPointApi.Entidades;
using MarketPointApi.Utilidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketPointApi.Controllers
{
    [ApiController]
    [Route("api/misCompras")]
    public class MisComprasController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public MisComprasController(UserManager<IdentityUser> userManager, 
            ApplicationDbContext context,
            IMapper mapper)
        {
            this.userManager = userManager;
            this.context = context;
            this.mapper = mapper;
        }


        [HttpPost("traerProductos")]
        public async Task<ActionResult<LandingMisComprasDTO>> traerProductos([FromBody] int[] pro)
        {
            var newList = pro.ToList();

            var enProceso = await context.MisCompras.Where(x => newList.Contains(x.ProductoId) && x.Vendido == false).ToListAsync();
            var listEnProceso = enProceso.Select(x => x.ProductoId).ToList();

            var Vendidas = await context.MisCompras.Where(x => newList.Contains(x.ProductoId) && x.Vendido == true).ToListAsync();
            var listVendidas = Vendidas.Select(x => x.ProductoId).ToList();


            var misComprasEnProceso = await context.Productos
                //Primero mapeamos a ProductosCategorias, desps de esa tabla, mapeamos a Categoria
                //De esta manera podemos utilizar los datos de las tablas producto y categoria, gracias a ProductosCategoria
                .Include(x => x.ProductosCategorias).ThenInclude(x => x.Categoria)
                .Include(x => x.ProductosVendedores).ThenInclude(x => x.Vendedor)
                .Where(x => listEnProceso.Contains(x.Id)).ToListAsync();

            if (misComprasEnProceso == null) { return NotFound(); }


            var misCompras = await context.Productos
                //Primero mapeamos a ProductosCategorias, desps de esa tabla, mapeamos a Categoria
                //De esta manera podemos utilizar los datos de las tablas producto y categoria, gracias a ProductosCategoria
                .Include(x => x.ProductosCategorias).ThenInclude(x => x.Categoria)
                .Include(x => x.ProductosVendedores).ThenInclude(x => x.Vendedor)
                .Where(x => listVendidas.Contains(x.Id)).ToListAsync();

            if (misCompras == null) { return NotFound(); }

            var misComprasEnProcesoDTO = mapper.Map<List<ProductoDTO>>(misComprasEnProceso);
            var misComprasDTO = mapper.Map<List<ProductoDTO>>(misCompras);

            var respuesta = new LandingMisComprasDTO();
            respuesta.enProceso = misComprasEnProcesoDTO;
            respuesta.Compras = misComprasDTO;

            return respuesta;

        }


        [HttpGet("compradoresCliente/{id:int}")]
        public async Task<ActionResult<List<MisComprasDTO>>> GetAllClientes(int id)
        {
            var clientesQueryable = context.MisCompras.AsQueryable();
            if (id != 0)
            {
                clientesQueryable = clientesQueryable.Where(x => x.ClienteId == id && x.EsCliente == true);
            }

            var misCompras = await clientesQueryable.ToListAsync();
            return mapper.Map<List<MisComprasDTO>>(misCompras);

        }

        [HttpGet("compradoresVendedor/{id:int}")]
        public async Task<ActionResult<List<MisComprasDTO>>> GetAllVendedores(int id)
        {

            var vendedoresQueryable = context.MisCompras.AsQueryable();
            if (id != 0)
            {
                vendedoresQueryable = vendedoresQueryable.Where(x => x.VendedorId == id && x.EsCliente == false);
            }

            var misCompras = await vendedoresQueryable.ToListAsync();
            return mapper.Map<List<MisComprasDTO>>(misCompras);

        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] MisComprasDTO comprasDTO)
        {
            var compra = mapper.Map<MiCompra>(comprasDTO);

            context.Add(compra);
            await context.SaveChangesAsync();
            return NoContent();
        }

    }
}
