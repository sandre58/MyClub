using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyClub.PlayUp.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261123220016_AddSlotsStageEntryDeferrableUnique : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EF cannot model DEFERRABLE UNIQUE; an immediate unique index would break ApplyResolvedEntry
        // (ClearEntry then assign within one SaveChanges).
        migrationBuilder.Sql(
            """
            DO $guard$
            DECLARE duplicate_count integer;
            BEGIN
                SELECT COUNT(*) INTO duplicate_count
                FROM (
                    SELECT stage_id, entry_id
                    FROM slots
                    WHERE entry_id IS NOT NULL
                    GROUP BY stage_id, entry_id
                    HAVING COUNT(*) > 1
                ) AS duplicates;

                IF duplicate_count > 0 THEN
                    RAISE EXCEPTION
                        'Cannot add AK_slots_stage_id_entry_id: % duplicate (stage_id, entry_id) pairs exist among occupied slots.',
                        duplicate_count;
                END IF;
            END
            $guard$;

            ALTER TABLE slots
            ADD CONSTRAINT "AK_slots_stage_id_entry_id"
            UNIQUE (stage_id, entry_id)
            DEFERRABLE INITIALLY DEFERRED;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE slots
            DROP CONSTRAINT IF EXISTS "AK_slots_stage_id_entry_id";
            """);
    }
}
