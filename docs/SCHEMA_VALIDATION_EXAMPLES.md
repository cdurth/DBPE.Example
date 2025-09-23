# Schema Validation Examples

The system now enforces shallow contract structures (max 2 levels deep) to ensure optimal querying and management.

## ✅ Valid Contract Examples

### **Level 1: Flat Structure (Recommended)**
```csharp
public class OrderContract : IContract
{
    // ✅ All primitive/simple types - Level 1
    public int OrderId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }  // Enum is fine
    
    // ✅ Collections of simple types - Level 1
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}
```

### **Level 2: Shallow Nesting (Acceptable)**
```csharp
public class SubscriptionContract : IContract
{
    // Level 1: Root properties
    public int SubscriptionId { get; set; }
    public string CustomerEmail { get; set; }
    public decimal MonthlyAmount { get; set; }
    
    // ✅ Level 2: Simple value objects (max depth reached)
    public BillingInfo Billing { get; set; }
    public List<Feature> Features { get; set; } = new();
}

// ✅ Level 2: Simple value objects with primitives only
public class BillingInfo
{
    public string Currency { get; set; }
    public PaymentMethod Method { get; set; }  // Enum
    public int BillingDay { get; set; }
    // ✅ No nested objects here - stays at Level 2
}

public class Feature
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public int Limit { get; set; }
    // ✅ No nested objects here - stays at Level 2
}
```

## ❌ Invalid Contract Examples (Will Throw Exceptions)

### **Level 3: Too Deep - REJECTED**
```csharp
public class OrderContract : IContract
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; }
    
    // ❌ This will cause validation error
    public Customer Customer { get; set; }  // Level 2
}

public class Customer 
{
    public string Name { get; set; }
    public Address ShippingAddress { get; set; }  // ❌ Level 3 - TOO DEEP!
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}
```

**Error thrown:**
```
InvalidContractException: Contract 'OrderContract' property 'Customer.ShippingAddress' exceeds maximum depth of 2 levels. A shallow contract structure is required, max 2 levels deep. Current depth: 3, Property type: Address
```

### **Collection with Deep Nesting - REJECTED**
```csharp
public class InvoiceContract : IContract
{
    public int InvoiceId { get; set; }
    
    // ❌ This will cause validation error
    public List<InvoiceItem> Items { get; set; } = new();  // Level 2 (collection)
}

public class InvoiceItem
{
    public string ProductName { get; set; }
    public ProductDetails Details { get; set; }  // ❌ Level 3 - TOO DEEP!
}

public class ProductDetails
{
    public string Description { get; set; }
    public decimal Weight { get; set; }
}
```

**Error thrown:**
```
InvalidContractException: Contract 'InvoiceContract' property 'Items[Item].Details' exceeds maximum depth of 2 levels. A shallow contract structure is required, max 2 levels deep. Current depth: 3, Property type: ProductDetails
```

## 🔧 How to Fix Deep Contracts

### **Flatten the Structure**
```csharp
// ❌ Before: Deep nesting
public class OrderContract : IContract
{
    public Customer Customer { get; set; }
}
public class Customer 
{
    public Address ShippingAddress { get; set; }
}

// ✅ After: Flattened structure
public class OrderContract : IContract
{
    // Flatten Customer properties
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    
    // Flatten Address properties  
    public string ShippingStreet { get; set; }
    public string ShippingCity { get; set; }
    public string ShippingZip { get; set; }
    public string ShippingCountry { get; set; }
}
```

### **Use JSONB for Complex Data**
```csharp
// ✅ Store complex nested data as JSONB
public class OrderContract : IContract
{
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; }
    
    // Complex data stored as JSONB - no depth validation needed
    public Dictionary<string, object> CustomerDetails { get; set; } = new();
    public Dictionary<string, object> ShippingAddress { get; set; } = new();
}

// Usage:
var order = new OrderContract
{
    OrderId = 123,
    CustomerEmail = "john@example.com",
    CustomerDetails = new Dictionary<string, object>
    {
        ["name"] = "John Doe",
        ["phone"] = "+1234567890",
        ["preferences"] = new { newsletter = true, sms = false }
    },
    ShippingAddress = new Dictionary<string, object>
    {
        ["street"] = "123 Main St",
        ["city"] = "Anytown",
        ["coordinates"] = new { lat = 40.7128, lng = -74.0060 }
    }
};
```

## 🎯 Best Practices

### **1. Design for Querying**
```csharp
// ✅ Keep searchable fields as direct properties
public class UserContract : IContract
{
    public string UserId { get; set; }       // Queryable
    public string Email { get; set; }        // Queryable  
    public string CompanyName { get; set; }  // Queryable
    public DateTime CreatedAt { get; set; }  // Queryable
    public UserStatus Status { get; set; }   // Queryable
    
    // Complex data as JSONB
    public Dictionary<string, object> Profile { get; set; } = new();
    public List<Permission> Permissions { get; set; } = new(); // Simple objects
}
```

### **2. Validation Triggers**
Validation occurs when:
- **First message processed** for a contract type
- **Schema changes detected** (hash comparison)
- **Table creation/migration** happens

### **3. Development Workflow**
```csharp
// 1. Define your contract
public class MyContract : IContract { /* ... */ }

// 2. Send first message - validation happens automatically
await messageBus.Send(new MyContract { /* ... */ });

// 3. If validation fails, you'll get a clear error message
// 4. Fix the contract structure and try again
```

## 📊 Generated Table Structure

For valid contracts, tables are created automatically:

```sql
-- From SubscriptionContract example above
CREATE TABLE messaging_tracking.subscription_messages (
    id UUID PRIMARY KEY,
    tracked_message_id UUID NOT NULL REFERENCES messaging_tracking.tracked_messages(id),
    subscription_id INTEGER,
    customer_email TEXT,
    monthly_amount DECIMAL(19,4),
    billing JSONB,        -- BillingInfo object as JSON
    features JSONB        -- List<Feature> as JSON array
);
```

This validation ensures your contracts remain queryable, maintainable, and performant for web-based management interfaces! 🚀