using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations;

/// <inheritdoc />
public partial class ConvertRiskEvaluationIdsToGuid : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "public");

        migrationBuilder.CreateTable(
            name: "customer_risk_evaluations",
            schema: "public",
            columns: table => new
            {
                evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                overall_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                evaluated_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_customer_risk_evaluations", x => x.evaluation_id));

        migrationBuilder.CreateTable(
            name: "efa_configurations",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                year = table.Column<int>(type: "integer", nullable: false),
                efa_rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_by = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_efa_configurations", x => x.id));

        migrationBuilder.CreateTable(
            name: "export_audits",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                exported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                exported_by = table.Column<Guid>(type: "uuid", nullable: false),
                url = table.Column<string>(type: "text", nullable: false),
                category = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_export_audits", x => x.id));

        migrationBuilder.CreateTable(
            name: "industries",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_industries", x => x.id));

        migrationBuilder.CreateTable(
            name: "organizations",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                address = table.Column<string>(type: "text", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_organizations", x => x.id));

        migrationBuilder.CreateTable(
            name: "password_reset_tokens",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                token = table.Column<string>(type: "text", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_used = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_password_reset_tokens", x => x.id));

        migrationBuilder.CreateTable(
            name: "pd_temp_datas",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                pd_setup_json = table.Column<string>(type: "text", nullable: false),
                created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                updated_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_pd_temp_datas", x => x.id));

        migrationBuilder.CreateTable(
            name: "Permissions",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_permissions", x => x.id));

        migrationBuilder.CreateTable(
            name: "product_categories",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_product_categories", x => x.id));

        migrationBuilder.CreateTable(
            name: "risk_indicators",
            schema: "public",
            columns: table => new
            {
                indicator_id = table.Column<Guid>(type: "uuid", nullable: false),
                category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                possible_values = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Yes,No,N/A"),
                display_order = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_risk_indicators", x => x.indicator_id));

        migrationBuilder.CreateTable(
            name: "Roles",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                is_system_role = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_roles", x => x.id));

        migrationBuilder.CreateTable(
            name: "segment_masters",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                segment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                sub_segments = table.Column<List<string>>(type: "text[]", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_segment_masters", x => x.id));

        migrationBuilder.CreateTable(
            name: "UploadedFiles",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                stored_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                physical_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                public_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_uploaded_files", x => x.id));

        migrationBuilder.CreateTable(
            name: "branches",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                branch_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                branch_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                address = table.Column<string>(type: "TEXT", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_branches", x => x.id);
                table.ForeignKey(
                    name: "fk_branches_organizations_organization_id",
                    column: x => x.organization_id,
                    principalSchema: "public",
                    principalTable: "organizations",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "segments",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                product_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_segments", x => x.id);
                table.ForeignKey(
                    name: "fk_segments_product_categories_product_category_id",
                    column: x => x.product_category_id,
                    principalSchema: "public",
                    principalTable: "product_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "customer_risk_indicator_evaluations",
            schema: "public",
            columns: table => new
            {
                eval_detail_id = table.Column<Guid>(type: "uuid", nullable: false),
                evaluation_id = table.Column<Guid>(type: "uuid", nullable: false),
                indicator_id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer_risk_indicator_evaluations", x => x.eval_detail_id);
                table.ForeignKey(
                    name: "fk_customer_risk_indicator_evaluations_customer_risk_evaluatio",
                    column: x => x.evaluation_id,
                    principalSchema: "public",
                    principalTable: "customer_risk_evaluations",
                    principalColumn: "evaluation_id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_customer_risk_indicator_evaluations_risk_indicators_indicat",
                    column: x => x.indicator_id,
                    principalSchema: "public",
                    principalTable: "risk_indicators",
                    principalColumn: "indicator_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => x.id);
                table.ForeignKey(
                    name: "fk_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalSchema: "public",
                    principalTable: "Permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalSchema: "public",
                    principalTable: "Roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "users",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                first_name = table.Column<string>(type: "text", nullable: false),
                last_name = table.Column<string>(type: "text", nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                user_status = table.Column<int>(type: "integer", nullable: false),
                is_temporary_password = table.Column<bool>(type: "boolean", nullable: false),
                is_wizard_complete = table.Column<bool>(type: "boolean", nullable: false),
                branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.ForeignKey(
                    name: "fk_users_branches_branch_id",
                    column: x => x.branch_id,
                    principalSchema: "public",
                    principalTable: "branches",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "scenarios",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                segment_id = table.Column<Guid>(type: "uuid", nullable: false),
                scenario_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                probability = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                contractual_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                last_quarter_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                other_cash_flows_enabled = table.Column<bool>(type: "boolean", nullable: false),
                collateral_value_enabled = table.Column<bool>(type: "boolean", nullable: false),
                uploaded_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_scenarios", x => x.id);
                table.ForeignKey(
                    name: "fk_scenarios_segments_segment_id",
                    column: x => x.segment_id,
                    principalSchema: "public",
                    principalTable: "segments",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_scenarios_uploaded_files_uploaded_file_id",
                    column: x => x.uploaded_file_id,
                    principalSchema: "public",
                    principalTable: "UploadedFiles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                token = table.Column<string>(type: "text", nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "todo_items",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                labels = table.Column<List<string>>(type: "text[]", nullable: false),
                is_completed = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                priority = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_todo_items", x => x.id);
                table.ForeignKey(
                    name: "fk_todo_items_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_roles", x => x.id);
                table.ForeignKey(
                    name: "fk_user_roles_roles_role_id",
                    column: x => x.role_id,
                    principalSchema: "public",
                    principalTable: "Roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_user_roles_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_branches_branch_code",
            schema: "public",
            table: "branches",
            column: "branch_code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_branches_is_active",
            schema: "public",
            table: "branches",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "ix_branches_organization_id",
            schema: "public",
            table: "branches",
            column: "organization_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_evaluations_customer_date",
            schema: "public",
            table: "customer_risk_evaluations",
            columns: ["customer_number", "evaluation_date"]);

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_evaluations_customer_number",
            schema: "public",
            table: "customer_risk_evaluations",
            column: "customer_number");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_evaluation_id",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            column: "evaluation_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_indicator_id",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            column: "indicator_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_risk_indicator_evaluations_unique",
            schema: "public",
            table: "customer_risk_indicator_evaluations",
            columns: ["evaluation_id", "indicator_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EfaConfigurations_Year",
            schema: "public",
            table: "efa_configurations",
            column: "year",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_industries_name",
            schema: "public",
            table: "industries",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Code",
            schema: "public",
            table: "organizations",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_Email",
            schema: "public",
            table: "organizations",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Organizations_IsActive",
            schema: "public",
            table: "organizations",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Category",
            schema: "public",
            table: "Permissions",
            column: "category");

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Key",
            schema: "public",
            table: "Permissions",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_product_categories_type_name",
            schema: "public",
            table: "product_categories",
            columns: ["type", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id",
            schema: "public",
            table: "refresh_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_risk_indicators_category_display_order",
            schema: "public",
            table: "risk_indicators",
            columns: ["category", "display_order"]);

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionId",
            schema: "public",
            table: "RolePermissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_RoleId",
            schema: "public",
            table: "RolePermissions",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_RoleId_PermissionId",
            schema: "public",
            table: "RolePermissions",
            columns: ["role_id", "permission_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Roles_IsActive",
            schema: "public",
            table: "Roles",
            column: "is_active");

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            schema: "public",
            table: "Roles",
            column: "name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_segment_id",
            schema: "public",
            table: "scenarios",
            column: "segment_id");

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_segment_id_scenario_name",
            schema: "public",
            table: "scenarios",
            columns: ["segment_id", "scenario_name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_scenarios_uploaded_file_id",
            schema: "public",
            table: "scenarios",
            column: "uploaded_file_id");

        migrationBuilder.CreateIndex(
            name: "ix_segments_product_category_id",
            schema: "public",
            table: "segments",
            column: "product_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_segments_product_category_id_name",
            schema: "public",
            table: "segments",
            columns: ["product_category_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_todo_items_user_id",
            schema: "public",
            table: "todo_items",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            schema: "public",
            table: "UserRoles",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_UserId",
            schema: "public",
            table: "UserRoles",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_UserId_RoleId",
            schema: "public",
            table: "UserRoles",
            columns: ["user_id", "role_id"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_branch_id",
            schema: "public",
            table: "users",
            column: "branch_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            schema: "public",
            table: "users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "customer_risk_indicator_evaluations",
            schema: "public");

        migrationBuilder.DropTable(
            name: "efa_configurations",
            schema: "public");

        migrationBuilder.DropTable(
            name: "export_audits",
            schema: "public");

        migrationBuilder.DropTable(
            name: "industries",
            schema: "public");

        migrationBuilder.DropTable(
            name: "password_reset_tokens",
            schema: "public");

        migrationBuilder.DropTable(
            name: "pd_temp_datas",
            schema: "public");

        migrationBuilder.DropTable(
            name: "refresh_tokens",
            schema: "public");

        migrationBuilder.DropTable(
            name: "RolePermissions",
            schema: "public");

        migrationBuilder.DropTable(
            name: "scenarios",
            schema: "public");

        migrationBuilder.DropTable(
            name: "segment_masters",
            schema: "public");

        migrationBuilder.DropTable(
            name: "todo_items",
            schema: "public");

        migrationBuilder.DropTable(
            name: "UserRoles",
            schema: "public");

        migrationBuilder.DropTable(
            name: "customer_risk_evaluations",
            schema: "public");

        migrationBuilder.DropTable(
            name: "risk_indicators",
            schema: "public");

        migrationBuilder.DropTable(
            name: "Permissions",
            schema: "public");

        migrationBuilder.DropTable(
            name: "segments",
            schema: "public");

        migrationBuilder.DropTable(
            name: "UploadedFiles",
            schema: "public");

        migrationBuilder.DropTable(
            name: "Roles",
            schema: "public");

        migrationBuilder.DropTable(
            name: "users",
            schema: "public");

        migrationBuilder.DropTable(
            name: "product_categories",
            schema: "public");

        migrationBuilder.DropTable(
            name: "branches",
            schema: "public");

        migrationBuilder.DropTable(
            name: "organizations",
            schema: "public");
    }
}
