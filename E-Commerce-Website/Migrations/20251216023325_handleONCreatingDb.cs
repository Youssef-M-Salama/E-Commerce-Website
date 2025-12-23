using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_Commerce_Website.Migrations
{
    /// <inheritdoc />
    public partial class handleONCreatingDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Admins",
                columns: new[] { "AdminId", "AdminEmail", "AdminImage", "AdminName", "AdminPassword" },
                values: new object[] { 98798791, "admin@example.com", null, "SuperAdmin", "Admin@123" });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "CustomerId", "CustomerAddress", "CustomerCity", "CustomerCountry", "CustomerEmail", "CustomerGender", "CustomerImage", "CustomerName", "CustomerPassword", "CustomerPhone" },
                values: new object[] { 187987987, null, null, null, "customer@example.com", null, null, "Default Customer", "Customer@123", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 98798791);

            migrationBuilder.DeleteData(
                table: "Customers",
                keyColumn: "CustomerId",
                keyValue: 187987987);
        }
    }
}
