using MDMS.Domain.Entities;
using MDMS.Domain.Enums;

namespace MDMS.Tests.Domain;

public class RevenueProtectionLeadTests
{
    [Fact]
    public void Detect_StartsAtDetectedWithZeroScore()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);

        Assert.Equal(RevenueProtectionLeadStatus.Detected, lead.Status);
        Assert.Equal(0m, lead.RiskScore);
    }

    [Fact]
    public void AddScore_FirstSignal_MovesToScored()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);

        lead.AddScore(20m);

        Assert.Equal(RevenueProtectionLeadStatus.Scored, lead.Status);
        Assert.Equal(20m, lead.RiskScore);
    }

    [Fact]
    public void AddScore_AccumulatesAcrossMultipleSignals()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);

        lead.AddScore(20m);
        lead.AddScore(15m);

        Assert.Equal(35m, lead.RiskScore);
        Assert.Equal(RevenueProtectionLeadStatus.Scored, lead.Status); // stays Scored, doesn't reset
    }

    [Fact]
    public void FullHappyPath_WithRecovery_ReachesClosed()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), Guid.NewGuid());
        lead.AddScore(30m);

        lead.Review();
        lead.AssignTo(Guid.NewGuid());
        lead.StartFieldInvestigation();
        lead.RecordFinding("Meter bypass found on inspection.");
        lead.RecordAction("Meter replaced, case referred for assessment.");
        lead.RecordRecovery(15000m);
        lead.Close("Confirmed unauthorized consumption; recovery completed.");

        Assert.Equal(RevenueProtectionLeadStatus.Closed, lead.Status);
        Assert.Equal(15000m, lead.RecoveryAmount);
    }

    [Fact]
    public void Close_DirectlyFromActionTaken_SucceedsForFalsePositive()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);
        lead.AddScore(20m);
        lead.Review();
        lead.AssignTo(Guid.NewGuid());
        lead.StartFieldInvestigation();
        lead.RecordFinding("No irregularity found.");
        lead.RecordAction("No action required.");

        lead.Close("False positive - closed without recovery.");

        Assert.Equal(RevenueProtectionLeadStatus.Closed, lead.Status);
        Assert.Null(lead.RecoveryAmount);
    }

    [Fact]
    public void Close_FromReviewed_Throws()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);
        lead.AddScore(20m);
        lead.Review();

        Assert.Throws<InvalidOperationException>(() => lead.Close("too early"));
    }

    [Fact]
    public void AddScore_OnClosedLead_Throws()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);
        lead.AddScore(20m);
        lead.Review();
        lead.AssignTo(Guid.NewGuid());
        lead.StartFieldInvestigation();
        lead.RecordFinding("finding");
        lead.RecordAction("action");
        lead.Close("closed");

        Assert.Throws<InvalidOperationException>(() => lead.AddScore(10m));
    }

    [Fact]
    public void RecordFinding_BlankNote_Throws()
    {
        var lead = RevenueProtectionLead.Detect(Guid.NewGuid(), null);
        lead.AddScore(10m);
        lead.Review();
        lead.AssignTo(Guid.NewGuid());
        lead.StartFieldInvestigation();

        Assert.Throws<ArgumentException>(() => lead.RecordFinding(" "));
    }
}
