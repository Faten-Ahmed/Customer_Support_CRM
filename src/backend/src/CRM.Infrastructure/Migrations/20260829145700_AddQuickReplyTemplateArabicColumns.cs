using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuickReplyTemplateArabicColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuickReplyTemplates_Scope_CreatedByUserId",
                table: "QuickReplyTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuickReplyTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AddColumn<string>(
                name: "ContentAr",
                table: "QuickReplyTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "QuickReplyTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "QuickReplyTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_QuickReplyTemplates_Scope_CreatedByUserId",
                table: "QuickReplyTemplates",
                columns: new[] { "Scope", "CreatedByUserId" },
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuickReplyTemplates_Scope_CreatedByUserId",
                table: "QuickReplyTemplates");

            migrationBuilder.DropColumn(
                name: "ContentAr",
                table: "QuickReplyTemplates");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "QuickReplyTemplates");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "QuickReplyTemplates");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "QuickReplyTemplates",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_QuickReplyTemplates_Scope_CreatedByUserId",
                table: "QuickReplyTemplates",
                columns: new[] { "Scope", "CreatedByUserId" });
        }
    }
}
