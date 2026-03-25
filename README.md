# EnterBridge Procurement App

A Blazor Server web application for a building supplies procurement team to browse products, track pricing trends, and manage orders.

## How to Run

**Prerequisites:** .NET 10 SDK

```bash
# Clone and run
cd EnterBridge
dotnet run
```

Open `https://localhost:5001` (or the URL shown in console). No database setup needed -- SQLite is created automatically on first run.

## Architecture

- **Blazor Server (.NET 10)** -- chosen for real-time interactivity without a separate frontend build step. Server-side rendering keeps the API key/calls server-side.
- **EF Core + SQLite** -- lightweight, zero-config local database for order persistence. Schema auto-creates on startup via `EnsureCreated()`.
- **Bootstrap 5 + Bootstrap Icons** -- responsive, mobile-friendly UI that works on iPads and desktops without custom CSS complexity.
- **Scoped CartService** -- in-memory cart per Blazor circuit (user session). Prices are captured from the API at the moment of adding to cart, snapshotted into the order on submission.

## Data Model

```
Order
  - Id, SubmittedBy, CreatedAt, UpdatedAt, Status (Submitted/Approved/Rejected), Notes, ReviewedBy

OrderItem
  - Id, OrderId, ProductId, ProductName, ProductSku, Category
  - UnitPrice, UnitOfMeasure, Quantity (price snapshot at time of order)
  - IsRejected, RejectionReason (for foreman review)
```

Key decision: prices are **snapshotted** into OrderItem at order creation time, not referenced by foreign key. This ensures the order reflects the price the user saw, even if prices change weekly.

## What I Built

### Core Requirements
- **Product catalog** -- paginated grid with category filter and name search, "Add to Cart" from the listing
- **Product detail page** -- full product info with current price and visual price history chart
- **Shopping cart** -- adjust quantities, submit with user name and optional notes
- **Order persistence** -- SQLite database with EF Core, orders stored with price snapshots
- **User tracking** -- name captured at order submission, no auth system

### Open-Ended Features

**1. "Understand how prices are moving"**
*Interpretation:* The procurement manager wants to see price trends to time purchases. I built a **price history page** on each product detail view with:
- Visual bar chart of price over time (3M / 6M / 1Y / All toggles)
- Price change percentage vs. 3 months ago (up/down indicator)
- Detailed price table with week-over-week changes

**2. "Foreman reviews orders"**
*Interpretation:* Replace the text-message workflow with an in-app review system. I built an **order review page** where the foreman can:
- View all submitted orders, filter by status
- Adjust quantities on individual line items
- Reject individual items (with optional reason) or restore them
- Approve or reject the entire order
- Orders track who reviewed them and when

**3. "Make frequent orders easier"**
*Interpretation:* The team re-orders the same products regularly. I added a **"Reorder" button** on both the orders list and order detail pages. One click copies all non-rejected items from a past order into the cart at **current prices** (fetched fresh from the API), preserving original quantities. The user is taken directly to the cart to review and submit.

## Key Decisions

- **Blazor Server over Blazor WASM**: Server-side keeps API calls off the client and avoids CORS. Good fit for ~20 internal users on a stable network.
- **No authentication**: Per requirements. User name is entered at order submission. In production, I'd integrate with the company's identity provider.
- **Price snapshot on order**: Prices change weekly. The order must reflect what the user saw at purchase time, so prices are copied into the order record.
- **EnsureCreated over migrations**: For a case study, `EnsureCreated()` is simpler. In production, I'd use EF Core migrations for schema evolution.
- **CSS bar chart over JS charting library**: Keeps dependencies minimal. A production app would benefit from Chart.js or a Blazor charting library for interactivity.

## What I'd Do Next

1. **Deserialization bug** -- there appears to be a deserialization issue with some product info text where backslashes are being used for quotation marks and other characters
2. **Real-time price alerts** -- notify when a frequently-ordered product drops in price
3. **Proper auth** -- integrate with Azure AD or similar for role-based access (procurement vs. foreman)
4. **Better charting** -- Chart.js or similar for interactive price trend visualization
5. **Order export** -- PDF or CSV export for the foreman's records

## AI Tools Used

Built with assistance from Claude (Anthropic) via Claude Code for scaffolding, code generation, and iterating on the implementation.
