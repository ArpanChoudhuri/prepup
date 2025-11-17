using Application.Orders.Commands.CreateOrder;
using Application.Orders.Queries.GetOrdersByCustomer;
using Application.Orders.Queries.GetOrdersQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.OrderId }, result);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(string customerId)
        {
            var result = await _mediator.Send(new GetOrdersByCustomerQuery(customerId));
            return Ok(result);
        }


        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            // can be implemented later
            return Ok(new { id });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(Guid query)
        {
            var result = await _mediator.Send(new GetOrderQuery(query));
            return Ok(result);
        }
    }
}
