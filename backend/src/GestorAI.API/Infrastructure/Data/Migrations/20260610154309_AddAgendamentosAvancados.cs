using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestorAI.API.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgendamentosAvancados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "aprovar_automaticamente",
                table: "configuracoes_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "horas_limite_cancelamento",
                table: "configuracoes_empresa",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_sinal",
                table: "configuracoes_empresa",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sinal_asaas_id",
                table: "agendamentos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "sinal_pago",
                table: "agendamentos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "sinal_pix_qr_code",
                table: "agendamentos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aprovar_automaticamente",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "horas_limite_cancelamento",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "valor_sinal",
                table: "configuracoes_empresa");

            migrationBuilder.DropColumn(
                name: "sinal_asaas_id",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "sinal_pago",
                table: "agendamentos");

            migrationBuilder.DropColumn(
                name: "sinal_pix_qr_code",
                table: "agendamentos");
        }
    }
}
