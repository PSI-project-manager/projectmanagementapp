using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "itemtypes",
                columns: table => new
                {
                    itemtypeid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("itemtypes_pkey", x => x.itemtypeid);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    roleid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("roles_pkey", x => x.roleid);
                });

            migrationBuilder.CreateTable(
                name: "statuses",
                columns: table => new
                {
                    statusid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sortorder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("statuses_pkey", x => x.statusid);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    passwordhash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    fullname = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.userid);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    projectid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    isactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createdbyuserid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("projects_pkey", x => x.projectid);
                    table.ForeignKey(
                        name: "projects_createdbyuserid_fkey",
                        column: x => x.createdbyuserid,
                        principalTable: "users",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateTable(
                name: "userroles",
                columns: table => new
                {
                    userid = table.Column<int>(type: "integer", nullable: false),
                    roleid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("userroles_pkey", x => new { x.userid, x.roleid });
                    table.ForeignKey(
                        name: "userroles_roleid_fkey",
                        column: x => x.roleid,
                        principalTable: "roles",
                        principalColumn: "roleid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "userroles_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "items",
                columns: table => new
                {
                    itemid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    projectid = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    itemtypeid = table.Column<int>(type: "integer", nullable: false),
                    statusid = table.Column<int>(type: "integer", nullable: false),
                    assignedtouserid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("items_pkey", x => x.itemid);
                    table.ForeignKey(
                        name: "items_assignedtouserid_fkey",
                        column: x => x.assignedtouserid,
                        principalTable: "users",
                        principalColumn: "userid");
                    table.ForeignKey(
                        name: "items_itemtypeid_fkey",
                        column: x => x.itemtypeid,
                        principalTable: "itemtypes",
                        principalColumn: "itemtypeid");
                    table.ForeignKey(
                        name: "items_projectid_fkey",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "projectid");
                    table.ForeignKey(
                        name: "items_statusid_fkey",
                        column: x => x.statusid,
                        principalTable: "statuses",
                        principalColumn: "statusid");
                });

            migrationBuilder.CreateTable(
                name: "projectusers",
                columns: table => new
                {
                    projectid = table.Column<int>(type: "integer", nullable: false),
                    userid = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("projectusers_pkey", x => new { x.projectid, x.userid });
                    table.ForeignKey(
                        name: "projectusers_projectid_fkey",
                        column: x => x.projectid,
                        principalTable: "projects",
                        principalColumn: "projectid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "projectusers_userid_fkey",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_items_assignedtouserid",
                table: "items",
                column: "assignedtouserid");

            migrationBuilder.CreateIndex(
                name: "ix_items_itemtypeid",
                table: "items",
                column: "itemtypeid");

            migrationBuilder.CreateIndex(
                name: "ix_items_projectid",
                table: "items",
                column: "projectid");

            migrationBuilder.CreateIndex(
                name: "ix_items_statusid",
                table: "items",
                column: "statusid");

            migrationBuilder.CreateIndex(
                name: "ix_items_title",
                table: "items",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "itemtypes_name_key",
                table: "itemtypes",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_createdbyuserid",
                table: "projects",
                column: "createdbyuserid");

            migrationBuilder.CreateIndex(
                name: "ix_projectusers_userid",
                table: "projectusers",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "roles_name_key",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "statuses_name_key",
                table: "statuses",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_userroles_roleid",
                table: "userroles",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "items");

            migrationBuilder.DropTable(
                name: "projectusers");

            migrationBuilder.DropTable(
                name: "userroles");

            migrationBuilder.DropTable(
                name: "itemtypes");

            migrationBuilder.DropTable(
                name: "statuses");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
