using CfdWorkbench.Core;
using static CfdWorkbench.Core.Tests.IdentityTests;

namespace CfdWorkbench.Core.Tests;

/// <summary>HIST: the read-only accepted-source accessor holds every revision of the session's accepted history. Ring 0, ~0.2 s.</summary>
internal static class AcceptedSourceTests
{
    internal static void Run()
    {
        Check("AcceptedSourceOf_EarlierRevision_ReturnsItsOwnBytesNotTheCurrent", () =>
        {
            using var session = Opened();
            var first = session.Snapshot();
            session.ApplyDimension(Id(), new("span", "1350"));
            var second = session.Snapshot();
            Equal(false, first.AcceptedId == second.AcceptedId);
            Equal(true, session.AcceptedSourceOf(first.AcceptedId)!.AsSpan().SequenceEqual(first.Source));
            Equal(true, session.AcceptedSourceOf(second.AcceptedId)!.AsSpan().SequenceEqual(second.Source));
            Equal(false, first.Source.AsSpan().SequenceEqual(second.Source));
        });
        Check("AcceptedSourceOf_AfterUndo_StillHoldsTheUndoneRevision", () =>
        {
            using var session = Opened();
            string firstId = session.Snapshot().AcceptedId;
            session.ApplyDimension(Id(), new("span", "1350"));
            string secondId = session.Snapshot().AcceptedId;
            session.Undo(Id());
            Equal(firstId, session.Snapshot().AcceptedId);
            Equal(true, session.AcceptedSourceOf(secondId) is not null);
        });
        Check("AcceptedSourceOf_UnheldId_IsNullNeverTheCurrentRevision", () =>
        {
            using var session = Opened();
            Equal(true, session.AcceptedSourceOf("accepted-not-held") is null);
            Equal(true, session.AcceptedSourceOf("") is null);
        });
        Check("AcceptedSourceOf_ReopenedImage_HoldsTheSavedHistory", () =>
        {
            using var session = Opened();
            string firstId = session.Snapshot().AcceptedId;
            byte[] firstBytes = session.Snapshot().Source.ToArray();
            session.ApplyDimension(Id(), new("span", "1350"));
            using var reopened = new AuthoringSession();
            reopened.Reopen(session.SaveImage());
            Equal(true, reopened.AcceptedSourceOf(firstId)!.AsSpan().SequenceEqual(firstBytes));
        });
    }

    private static string Id() => Guid.NewGuid().ToString("D");

    private static AuthoringSession Opened()
    {
        var session = new AuthoringSession();
        session.Open(FoilSourceTests.Example, Id(), true);
        return session;
    }
}
