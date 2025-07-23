# EggLedger Database Schema Design

```mermaid
erDiagram
    User ||--o{ Container : "buys"
    User ||--o{ Order : "places"
    User ||--|| UserPassword : "has"
    User ||--o{ RefreshToken : "has"
    User ||--o{ UserRoom : "belongs_to"
    Room ||--o{ Container : "contains"
    Room ||--o{ UserRoom : "has_members"
    Container ||--o{ OrderDetail : "included_in"
    Order ||--o{ OrderDetail : "contains"
    Order ||--o{ Transaction : "has"
    
    User {
        Guid Id PK
        string FirstName
        string LastName
        string Email UK
        int Role
        string Provider
    }

    UserPassword {
        Guid Id PK
        Guid UserId FK
        string PasswordHash
    }

    RefreshToken {
        Guid Id PK
        Guid UserId FK
        string Token
        DateTime Expires
        DateTime Created
        string CreatedByIp
        DateTime Revoked
        string RevokedByIp
        string ReplacedByToken
    }

    Room {
        Guid Id PK
        string Name
        string Code UK
        bool IsPublic
        DateTime CreatedAt
    }

    UserRoom {
        Guid Id PK
        Guid UserId FK
        Guid RoomId FK
        bool IsAdmin
        DateTime JoinedAt
    }

    Container {
        Guid Id PK
        string Name
        DateTime PurchaseDateTime
        int TotalQuantity
        int RemainingQuantity
        decimal Amount
        Guid BuyerId FK
        Guid RoomId FK
    }

    Order {
        Guid Id PK
        string Name
        DateTime Datestamp
        int Quantity
        decimal Amount
        int Type
        int Status
        Guid UserId FK
    }

    OrderDetail {
        Guid Id PK
        int Quantity
        decimal Amount
        int Status
        Guid OrderId FK
        Guid ContainerId FK
    }

    Transaction {
        Guid Id PK
        DateTime Datestamp
        decimal Amount
        int Status
        Guid OrderId FK
        Guid PayerId FK
        Guid ReceiverId FK
    }
```

## Key Design Points

1. **User Management**
   - Users have secure password storage in a separate table
   - Refresh tokens handled separately for security
   - Users can be part of multiple rooms

2. **Room System**
   - Rooms have unique codes for identification
   - Public/Private room support
   - Admin privileges tracked per user per room

3. **Container Management**
   - Containers track both total and remaining quantities
   - Tied to specific rooms and buyers
   - Deletion of room cascades to containers

4. **Order System**
   - Orders can contain multiple order details
   - Order details link to specific containers
   - Orders track status and type

5. **Transaction System**
   - Transactions linked to orders
   - Separate tracking of payer and receiver
   - Status tracking for payment state

## Key Relationships

1. **User-Room Relationship**
   - Many-to-Many through UserRoom
   - Tracks admin status and join date
   - Cascade deletion for user room associations

2. **Container-Order Relationship**
   - Many-to-Many through OrderDetail
   - Tracks quantity and amount per order detail
   - Prevents container deletion when referenced by orders

3. **Transaction Flow**
   - Links payer and receiver users
   - Connected to specific orders
   - Cascading deletion with orders

## Database Constraints

1. **Unique Constraints**
   - Room codes are unique
   - User emails are unique
   - One password per user
   - One user can join a room only once

2. **Delete Behaviors**
   - Room deletion cascades to containers
   - User deletion cascades to refresh tokens
   - Order deletion cascades to details and transactions
   - Container references are protected from deletion when in use

This is a Low-Level Design (LLD) as it details:
- Exact database schema
- Field types and constraints
- Relationship cardinalities
- Delete behaviors
- Unique constraints
- Foreign key relationships
