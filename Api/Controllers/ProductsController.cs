using Application.Products.Commands.CreateProduct;
using Application.Products.Commands.Queries.GetProducts;
using Application.Products.Commands.UpdateProduct;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            // Implementation for getting all products
            var result = await _mediator.Send(new GetProductsCommand());
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            // Implementation for getting a product by ID
            if (id!=null )
            {
                var result= await _mediator.Send(new GetProductByIdCommand(id));
                return Ok(result);
            }
            return BadRequest(NoContent());
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand body)
        {
            // Implementation for creating a product
            if (body == null) {
                return BadRequest("Request body cannot be null.");
            }
            await _mediator.Send(body);
            return CreatedAtAction(nameof(GetProductById), new { id = Guid.NewGuid() }, null);
        }

        [HttpPut("{id:guid}/price")]
        public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdateProductPriceCommand body)
        {
            if (id != body.ProductId)
                return BadRequest("Product ID in the URL and body do not match.");

            await _mediator.Send(body);
            return NoContent();
        }



    }
}
