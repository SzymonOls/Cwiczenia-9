using Microsoft.AspNetCore.Mvc;
using API2.Model;
using Microsoft.Data.SqlClient;

namespace API2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly string _connectionString;

    public WarehouseController(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // [HttpGet("ping")]
    // public IActionResult Ping()
    // {
    //     return Ok("ok");
    // }
    
    
    [HttpPost("add-product")]
    public async Task<IActionResult> AddProductToWarehouse([FromBody] RequestDto request)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        using var transaction = connection.BeginTransaction();

        try
        {
            // czy produkt istnieje
            //Console.WriteLine("Database" + connection.Database);

            var productExists = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @id", connection, transaction);
            productExists.Parameters.AddWithValue("@id", request.ProductId);
            
            if (await productExists.ExecuteScalarAsync() == null)
                return NotFound("Product not found");
            var productPrice = Convert.ToDecimal(await productExists.ExecuteScalarAsync());

            // czy magazyn istnieje
            var warehouseExists = new SqlCommand("SELECT COUNT(*) FROM Warehouse WHERE IdWarehouse = @id", connection, transaction);
            warehouseExists.Parameters.AddWithValue("@id", request.WarehouseId);
            
            if (await warehouseExists.ExecuteScalarAsync() == null)
                return NotFound("Warehouse not found");

            // zamowienie
            var orderCommand = new SqlCommand(@"SELECT TOP 1 IdOrder FROM [Order] WHERE IdProduct = @productId AND Amount = @amount and CreatedAt < @createdAt order by CreatedAt asc",
                    connection, 
                    transaction);
            orderCommand.Parameters.AddWithValue("@productId", request.ProductId);
            orderCommand.Parameters.AddWithValue("@amount", request.Amount);
            orderCommand.Parameters.AddWithValue("@createdAt", request.CreatedAt);
            var orderIdObj = await orderCommand.ExecuteScalarAsync();
            if (orderIdObj == null)
            {
                //Console.WriteLine("Order not found");
                return NotFound("Order not found");
            }
            var orderId = (int)orderIdObj;

            // czy zrealizowane
            var isDone = new SqlCommand("select count(*) from Product_Warehouse where IdOrder = @orderId", connection, transaction);
            isDone.Parameters.AddWithValue("@orderId", orderId);
            
            var fulfilled = (int)await isDone.ExecuteScalarAsync() > 0;
            if (fulfilled)
                return BadRequest("Order is fulfilled");
            
            // update fulfilledAt
            var updateOrder = new SqlCommand("UPDATE [Order] SET FulfilledAt = @now WHERE IdOrder = @id", connection, transaction);
            updateOrder.Parameters.AddWithValue("@now", DateTime.Now);
            updateOrder.Parameters.AddWithValue("@id", orderId);

            await updateOrder.ExecuteNonQueryAsync();
            
            // wstawienie
            var insert = new SqlCommand(@"
                INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                OUTPUT INSERTED.IdProductWarehouse
                VALUES (@warehouseId, @productId, @orderId, @amount, @price, @createdAt)", connection, transaction);
            insert.Parameters.AddWithValue("@warehouseId", request.WarehouseId);
            insert.Parameters.AddWithValue("@productId", request.ProductId);
            insert.Parameters.AddWithValue("@orderId", orderId);
            insert.Parameters.AddWithValue("@amount", request.Amount);
            insert.Parameters.AddWithValue("@price", request.Amount * productPrice);
            insert.Parameters.AddWithValue("@createdAt", request.CreatedAt);
            
            var insertedId = await insert.ExecuteNonQueryAsync();
            
            transaction.Commit();
            
            return Ok(new { IdProductWarehouse = insertedId });
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return BadRequest(ex.Message);
        }
    }


    [HttpPost("add-product-proc")]
    public async Task<IActionResult> AddProductUsingProcedure([FromBody] RequestDto request)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = System.Data.CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.ProductId);
        command.Parameters.AddWithValue("@IdWarehouse", request.WarehouseId);
        command.Parameters.AddWithValue("@amount", request.Amount);
        command.Parameters.AddWithValue("@createdAt", request.CreatedAt);

        try
        {
            var result = await command.ExecuteScalarAsync();
            if (result == null)
                return StatusCode(500, "ID not returned");
            
            return Ok(new { IdProductWarehouse = Convert.ToInt32(result) });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}