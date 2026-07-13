using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReferWell.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCaseNoAndSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Resolve any duplicate CaseNo values before enforcing uniqueness.
            // Keeps the oldest row's CaseNo; renumbers later duplicates after the current max Ref- sequence.
            migrationBuilder.Sql(@"
DECLARE @maxSeq INT =
(
    SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(CaseNo, 5, 20) AS INT)), 0)
    FROM Referrals
    WHERE CaseNo LIKE 'Ref-%' AND TRY_CAST(SUBSTRING(CaseNo, 5, 20) AS INT) IS NOT NULL
);

;WITH dups AS
(
    SELECT Id,
           CreatedAt,
           ROW_NUMBER() OVER (PARTITION BY CaseNo ORDER BY CreatedAt, Id) AS rn
    FROM Referrals
),
toFix AS
(
    SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAt, Id) AS seq
    FROM dups
    WHERE rn > 1
)
UPDATE r
SET CaseNo = 'Ref-' + RIGHT('000000' + CAST(@maxSeq + f.seq AS VARCHAR(10)), 6)
FROM Referrals r
INNER JOIN toFix f ON f.Id = r.Id;
");

            migrationBuilder.DropIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals",
                column: "CaseNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_CaseNo",
                table: "Referrals",
                column: "CaseNo");
        }
    }
}
