using System;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations;

[DbContext(typeof(FootballManagerDbContext))]
[Migration("20260814120000_PushSubscriptionsAndFollows")]
public partial class PushSubscriptionsAndFollows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "push_subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                p256dh = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                auth = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_push_subscriptions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "push_follows",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                push_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                scope_type = table.Column<int>(type: "integer", nullable: false),
                scope_id = table.Column<Guid>(type: "uuid", nullable: false),
                notify_results = table.Column<bool>(type: "boolean", nullable: false),
                notify_fixture = table.Column<bool>(type: "boolean", nullable: false),
                notify_standings = table.Column<bool>(type: "boolean", nullable: false),
                notify_news = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_push_follows", x => x.id);
                table.ForeignKey(
                    name: "FK_push_follows_push_subscriptions_push_subscription_id",
                    column: x => x.push_subscription_id,
                    principalTable: "push_subscriptions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_push_subscriptions_endpoint",
            table: "push_subscriptions",
            column: "endpoint",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_push_subscriptions_is_active",
            table: "push_subscriptions",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_push_follows_push_subscription_id_scope_type_scope_id",
            table: "push_follows",
            columns: new[] { "push_subscription_id", "scope_type", "scope_id" },
            unique: true,
            filter: "deleted_at IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_push_follows_scope_type_scope_id",
            table: "push_follows",
            columns: new[] { "scope_type", "scope_id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "push_follows");
        migrationBuilder.DropTable(name: "push_subscriptions");
    }
}
