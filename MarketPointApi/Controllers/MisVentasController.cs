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
    [Route("api/misVentas")]
    public class MisVentasController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public MisVentasController(UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IMapper mapper)
        {
            this.userManager = userManager;
            this.context = context;
            this.mapper = mapper;
        }

        //
        //VENTAS EN PROCESO, POR LO TANTO VA A MISCOMPRASDTO
        [HttpGet("ventas/{id:int}")]
        public async Task<ActionResult<List<MisComprasDTO>>> GetAllVendedores(int id)
        {
            var vendedoresQueryable = context.MisCompras.AsQueryable();
            if (id != 0)
            {
                vendedoresQueryable = vendedoresQueryable.Where(x => x.VendedorId == id);
            }

            var misCompras = await vendedoresQueryable.ToListAsync();
            return mapper.Map<List<MisComprasDTO>>(misCompras);

        }

        //VENTAS OFICIALES, POR LO TANTO VA A MISVENTASDTO
        [HttpGet("ventasTwo/{id:int}")]
        public async Task<ActionResult<List<MisVentasDTO>>> GetAllVendedoresVentas(int id)
        {
            var vendedoresQueryable = context.MisVentasCompras.AsQueryable();
            if (id != 0)
            {
                vendedoresQueryable = vendedoresQueryable.Where(x => x.VendedorId == id);
            }

            var misVentas = await vendedoresQueryable.ToListAsync();
            return mapper.Map<List<MisVentasDTO>>(misVentas);

        }



        [HttpGet("misComprasCliente/{id:int}")]
        public async Task<ActionResult<List<MisVentasDTO>>> GetAllClientesCompras(int id)
        {
            var comprasQueryable = context.MisVentasCompras.AsQueryable();
            if (id != 0)
            {
                comprasQueryable = comprasQueryable.Where(x => x.ClienteId == id && x.esCliente == true);
            }

            var misCompras = await comprasQueryable.ToListAsync();
            return mapper.Map<List<MisVentasDTO>>(misCompras);
        }

        [HttpGet("misComprasVendedor/{id:int}")]
        public async Task<ActionResult<List<MisVentasDTO>>> GetAllVendedoresCompras(int id)
        {
            var comprasQueryable = context.MisVentasCompras.AsQueryable();
            if (id != 0)
            {
                comprasQueryable = comprasQueryable.Where(x => x.ClienteId == id && x.esCliente == false);
            }

            var misCompras = await comprasQueryable.ToListAsync();
            return mapper.Map<List<MisVentasDTO>>(misCompras);
        }



        [HttpPost("mostrarVentas")]
        public async Task<ActionResult<List<MisVentasDTO>>> GetProductos([FromBody] int[] pro)
        {
            var newList = pro.ToList();
            var enProceso = await context.MisVentasCompras.OrderBy(x => x.ProductoId).Where(x => newList.Contains(x.ProductoId)).ToListAsync();
            var listEnProceso = enProceso.Select(x => x.ProductoId).ToList();

            var misComprasEnProceso = await context.Productos
                //Primero mapeamos a ProductosCategorias, desps de esa tabla, mapeamos a Categoria
                //De esta manera podemos utilizar los datos de las tablas producto y categoria, gracias a ProductosCategoria
                .Where(x => listEnProceso.Contains(x.Id)).ToListAsync();

            if (misComprasEnProceso == null) { return NotFound(); }

            var lista = new List<MisVentasDTO>();
            foreach (var i in misComprasEnProceso)
            {
                foreach (var x in enProceso)
                {
                    lista.Add(new MisVentasDTO()
                    {
                        Id = x.Id,
                        ClienteId = x.ClienteId,
                        ProductoId = x.ProductoId,
                        esCliente = x.esCliente,
                        total = x.total,
                        cantidad = x.cantidad,
                        Nombre = i.Nombre,
                        Precio = i.Precio,
                        Descripcion = i.Descripcion,
                        CantidadDisponible = i.CantidadDisponible,
                        ImagenProducto = i.ImagenProducto
                    });
                    enProceso.RemoveAt(0);
                    break;
                }

            }

            return lista;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromForm] MisVentasDTO misVentasDTO)
        {
            var venta = mapper.Map<MiVentaCompra>(misVentasDTO);

            context.Add(venta);
            await context.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var compraEnProceso = await context.MisCompras.FirstOrDefaultAsync(x => x.Id == id);
            if (compraEnProceso == null)
            {
                return NotFound();
            }

            context.Remove(compraEnProceso);
            await context.SaveChangesAsync();
            return NoContent();

        }



    }
}
