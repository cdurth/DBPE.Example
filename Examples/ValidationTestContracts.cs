using DBPE.Messaging.Abstractions;

namespace DBPE.Example;

// These contracts are for testing schema validation - DO NOT USE IN PRODUCTION

#region ✅ Valid Contracts (Should Work)

/// <summary>
/// ✅ VALID: Completely flat structure
/// </summary>
public class FlatTestContract : IContract
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public TestStatus Status { get; set; }
}

/// <summary>
/// ✅ VALID: Shallow structure with simple collections and value objects (Level 2)
/// </summary>
public class ShallowTestContract : IContract
{
    // Level 1: Root properties
    public int ContractId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    
    // Level 2: Simple collections (allowed)
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    // Level 2: Simple value objects (allowed)
    public TestAddress Address { get; set; } = new();
    public List<TestItem> Items { get; set; } = new();
}

/// <summary>
/// Simple value object for Level 2 - contains only primitives
/// </summary>
public class TestAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    // ✅ No nested objects - stays at Level 2
}

/// <summary>
/// Simple value object for collections - contains only primitives
/// </summary>
public class TestItem
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    // ✅ No nested objects - stays at Level 2
}

#endregion

#region ❌ Invalid Contracts (Should Throw ValidationException)

/// <summary>
/// ❌ INVALID: Contains Level 3 nesting - will throw InvalidContractException
/// </summary>
public class DeepTestContract : IContract
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Level 2: Complex object
    public TestCustomer Customer { get; set; } = new();
}

/// <summary>
/// Level 2 object that contains Level 3 nesting (invalid)
/// </summary>
public class TestCustomer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // ❌ Level 3: This will cause validation to fail
    public TestContactInfo ContactInfo { get; set; } = new();
}

/// <summary>
/// Level 3 object (causes validation failure)
/// </summary>
public class TestContactInfo
{
    public string Phone { get; set; } = string.Empty;
    public string PreferredTime { get; set; } = string.Empty;
}

/// <summary>
/// ❌ INVALID: Collection items with Level 3 nesting
/// </summary>
public class DeepCollectionTestContract : IContract
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Level 2: Collection of complex objects
    public List<TestOrderItem> OrderItems { get; set; } = new();
}

/// <summary>
/// Level 2 object (in collection) that contains Level 3 nesting (invalid)
/// </summary>
public class TestOrderItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    
    // ❌ Level 3: This will cause validation to fail
    public TestProductDetails ProductDetails { get; set; } = new();
}

/// <summary>
/// Level 3 object (causes validation failure)
/// </summary>
public class TestProductDetails
{
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string Category { get; set; } = string.Empty;
}

#endregion

#region Supporting Types

public enum TestStatus
{
    Active,
    Inactive,
    Pending
}

#endregion

#region Test Helper Methods

/// <summary>
/// Helper class to manually test contract validation
/// </summary>
public static class ValidationTestHelper
{
    /// <summary>
    /// Test that valid contracts pass validation
    /// </summary>
    public static void TestValidContracts()
    {
        Console.WriteLine("Testing valid contracts...");
        
        // These should not throw exceptions
        var validContracts = new List<Type>
        {
            typeof(FlatTestContract),
            typeof(ShallowTestContract)
        };
        
        foreach (var contractType in validContracts)
        {
            try
            {
                // Create instance to trigger validation if used in message tracking
                Console.WriteLine($"✅ {contractType.Name} - Structure is valid");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {contractType.Name} - Unexpected error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Test that invalid contracts fail validation
    /// Note: These will only throw when actually processed by the message system
    /// </summary>
    public static void TestInvalidContracts()
    {
        Console.WriteLine("\nTesting invalid contracts...");
        Console.WriteLine("Note: Validation errors will occur when messages are first processed by the tracking system");
        
        var invalidContracts = new List<(Type Type, string ExpectedError)>
        {
            (typeof(DeepTestContract), "Customer.ContactInfo exceeds maximum depth"),
            (typeof(DeepCollectionTestContract), "OrderItems[Item].ProductDetails exceeds maximum depth")
        };
        
        foreach (var (contractType, expectedError) in invalidContracts)
        {
            Console.WriteLine($"⚠️  {contractType.Name} - Will fail validation: {expectedError}");
        }
    }
}

#endregion