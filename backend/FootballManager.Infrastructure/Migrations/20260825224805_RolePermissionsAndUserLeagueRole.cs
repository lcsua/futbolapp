using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RolePermissionsAndUserLeagueRole : Migration
    {
        private static readonly Guid AdminRoleId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        private static readonly Guid CargaRoleId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'CreatedAt') THEN
        ALTER TABLE roles RENAME COLUMN ""CreatedAt"" TO created_at;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'created_at') THEN
        ALTER TABLE roles ADD COLUMN created_at timestamptz NOT NULL DEFAULT NOW();
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'UpdatedAt') THEN
        ALTER TABLE roles RENAME COLUMN ""UpdatedAt"" TO updated_at;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'updated_at') THEN
        ALTER TABLE roles ADD COLUMN updated_at timestamptz NOT NULL DEFAULT NOW();
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'DeletedAt') THEN
        ALTER TABLE roles RENAME COLUMN ""DeletedAt"" TO deleted_at;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'roles' AND column_name = 'deleted_at') THEN
        ALTER TABLE roles ADD COLUMN deleted_at timestamptz;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'user_leagues' AND column_name = 'UpdatedAt') THEN
        ALTER TABLE user_leagues RENAME COLUMN ""UpdatedAt"" TO updated_at;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'user_leagues' AND column_name = 'updated_at') THEN
        ALTER TABLE user_leagues ADD COLUMN updated_at timestamptz NOT NULL DEFAULT NOW();
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'user_leagues' AND column_name = 'DeletedAt') THEN
        ALTER TABLE user_leagues RENAME COLUMN ""DeletedAt"" TO deleted_at;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'user_leagues' AND column_name = 'deleted_at') THEN
        ALTER TABLE user_leagues ADD COLUMN deleted_at timestamptz;
    END IF;
END $$;

ALTER TABLE roles ADD COLUMN IF NOT EXISTS code varchar(30);
ALTER TABLE roles ADD COLUMN IF NOT EXISTS is_system boolean NOT NULL DEFAULT false;
ALTER TABLE roles ADD COLUMN IF NOT EXISTS league_id uuid;
ALTER TABLE user_leagues ADD COLUMN IF NOT EXISTS role_id uuid;

DROP INDEX IF EXISTS ""IX_roles_name"";
");

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_leagues_role_id",
                table: "user_leagues",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_league_id_name",
                table: "roles",
                columns: new[] { "league_id", "name" },
                unique: true,
                filter: "league_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true,
                filter: "league_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.AddForeignKey(
                name: "FK_roles_leagues_league_id",
                table: "roles",
                column: "league_id",
                principalTable: "leagues",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_leagues_roles_role_id",
                table: "user_leagues",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            SeedPermissionsAndRoles(migrationBuilder);
        }

        private static void SeedPermissionsAndRoles(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;
            var catalog = new (Guid Id, string Code, string Name, string Module)[]
            {
                (Guid.Parse("b0000000-0000-0000-0000-000000000001"), "leagues", "Leagues", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000002"), "seasons", "Seasons", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000003"), "season_setup", "Season setup", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000004"), "divisions", "Divisions", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000005"), "teams", "Teams", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000006"), "clubs", "Clubs", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000007"), "fields", "Fields", "organization"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000008"), "fixtures", "Fixtures", "competition"),
                (Guid.Parse("b0000000-0000-0000-0000-000000000009"), "matches", "Matches", "competition"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000a"), "standings", "Standings", "competition"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000b"), "competition_rules", "Competition rules", "settings"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000c"), "match_rules", "Match rules", "settings"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000d"), "users", "Users", "admin"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000e"), "roles", "Roles", "admin"),
                (Guid.Parse("b0000000-0000-0000-0000-00000000000f"), "documents", "Documents", "organization"),
            };

            foreach (var item in catalog)
            {
                migrationBuilder.InsertData(
                    table: "permissions",
                    columns: new[] { "id", "code", "name", "module", "created_at", "updated_at", "deleted_at" },
                    values: new object[] { item.Id, item.Code, item.Name, item.Module, now, now, null });
            }

            migrationBuilder.Sql($@"
INSERT INTO roles (id, name, description, code, is_system, league_id, created_at, updated_at)
SELECT '{AdminRoleId}', 'Administrador', 'Acceso completo a la liga', 'ADMIN', TRUE, NULL, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM roles WHERE code = 'ADMIN');

INSERT INTO roles (id, name, description, code, is_system, league_id, created_at, updated_at)
SELECT '{CargaRoleId}', 'Carga', 'Carga de partidos y posiciones', 'CARGA', TRUE, NULL, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM roles WHERE code = 'CARGA');
");

            var cargaPermissionIds = new[]
            {
                Guid.Parse("b0000000-0000-0000-0000-000000000009"),
                Guid.Parse("b0000000-0000-0000-0000-00000000000a"),
            };

            foreach (var permissionId in catalog.Select(c => c.Id))
            {
                migrationBuilder.Sql($@"
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, '{permissionId}'
FROM roles r
WHERE r.code = 'ADMIN'
ON CONFLICT DO NOTHING;
");
            }

            foreach (var permissionId in cargaPermissionIds)
            {
                migrationBuilder.Sql($@"
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, '{permissionId}'
FROM roles r
WHERE r.code = 'CARGA'
ON CONFLICT DO NOTHING;
");
            }

            migrationBuilder.Sql(@"
UPDATE user_leagues ul
SET role_id = r.id
FROM roles r
WHERE ul.role_id IS NULL
  AND r.code = 'ADMIN';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_roles_leagues_league_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_user_leagues_roles_role_id",
                table: "user_leagues");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropIndex(
                name: "IX_user_leagues_role_id",
                table: "user_leagues");

            migrationBuilder.DropIndex(
                name: "IX_roles_code",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_league_id_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_name",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "user_leagues");

            migrationBuilder.DropColumn(
                name: "code",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_system",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "league_id",
                table: "roles");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);
        }
    }
}
