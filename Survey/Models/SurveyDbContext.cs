using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Survey.Models;

public partial class SurveyDbContext : DbContext
{
    public SurveyDbContext(DbContextOptions<SurveyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<BranchLogic> BranchLogics { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<ResponseAnswer> ResponseAnswers { get; set; }

    public virtual DbSet<ResponseAnswerOption> ResponseAnswerOptions { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<SurveyChannel> SurveyChannels { get; set; }

    public virtual DbSet<SurveyCollaborator> SurveyCollaborators { get; set; }

    public virtual DbSet<SurveyResponse> SurveyResponses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Activity__5E5486485438AD28");

            entity.ToTable("ActivityLog");

            entity.Property(e => e.ActionType).HasMaxLength(100);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Response).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.ResponseId)
                .HasConstraintName("FK_ActivityLog_Responses");

            entity.HasOne(d => d.Survey).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_ActivityLog_Surveys");

            entity.HasOne(d => d.User).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ActivityLog_Users");
        });

        modelBuilder.Entity<BranchLogic>(entity =>
        {
            entity.HasKey(e => e.LogicId).HasName("PK__BranchLo__4A718C1DDA872353");

            entity.ToTable("BranchLogic");

            entity.Property(e => e.LogicId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ConditionExpr).HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PriorityOrder).HasDefaultValue(1);
            entity.Property(e => e.TargetAction).HasMaxLength(50);

            entity.HasOne(d => d.SourceQuestion).WithMany(p => p.BranchLogicSourceQuestions)
                .HasForeignKey(d => d.SourceQuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BranchLogic_SourceQ");

            entity.HasOne(d => d.Survey).WithMany(p => p.BranchLogics)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_BranchLogic_Surveys");

            entity.HasOne(d => d.TargetQuestion).WithMany(p => p.BranchLogicTargetQuestions)
                .HasForeignKey(d => d.TargetQuestionId)
                .HasConstraintName("FK_BranchLogic_TargetQ");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__Question__0DC06FACCD651F63");

            entity.Property(e => e.QuestionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.HelpText).HasMaxLength(500);
            entity.Property(e => e.QuestionType).HasMaxLength(50);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ValidationRule).HasMaxLength(500);

            entity.HasOne(d => d.Survey).WithMany(p => p.Questions)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_Questions_Surveys");
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("PK__Question__92C7A1FFB7B84025");

            entity.Property(e => e.OptionId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OptionText).HasMaxLength(500);
            entity.Property(e => e.OptionValue).HasMaxLength(200);

            entity.HasOne(d => d.Question).WithMany(p => p.QuestionOptions)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK_QuestionOptions_Questions");
        });

        modelBuilder.Entity<ResponseAnswer>(entity =>
        {
            entity.HasKey(e => new { e.ResponseId, e.QuestionId });

            entity.Property(e => e.NumericValue).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Question).WithMany(p => p.ResponseAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResponseAnswers_Questions");

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseAnswers)
                .HasForeignKey(d => d.ResponseId)
                .HasConstraintName("FK_ResponseAnswers_Responses");
        });

        modelBuilder.Entity<ResponseAnswerOption>(entity =>
        {
            entity.HasKey(e => new { e.ResponseId, e.QuestionId, e.OptionId });

            entity.Property(e => e.AdditionalText).HasMaxLength(500);

            entity.HasOne(d => d.Option).WithMany(p => p.ResponseAnswerOptions)
                .HasForeignKey(d => d.OptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RAO_QuestionOptions");

            entity.HasOne(d => d.Question).WithMany(p => p.ResponseAnswerOptions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RAO_Questions");

            entity.HasOne(d => d.Response).WithMany(p => p.ResponseAnswerOptions)
                .HasForeignKey(d => d.ResponseId)
                .HasConstraintName("FK_RAO_Responses");
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.SurveyId).HasName("PK__Surveys__A5481F7DD5B087E5");

            entity.Property(e => e.SurveyId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DefaultLanguage).HasMaxLength(20);
            entity.Property(e => e.QuotaBehavior).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Draft");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Owner).WithMany(p => p.Surveys)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Surveys_Users");
        });

        modelBuilder.Entity<SurveyChannel>(entity =>
        {
            entity.HasKey(e => e.ChannelId).HasName("PK__SurveyCh__38C3E8142425123F");

            entity.Property(e => e.ChannelId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ChannelType).HasMaxLength(50);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmailSubject).HasMaxLength(255);
            entity.Property(e => e.FullUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PublicUrlSlug).HasMaxLength(200);
            entity.Property(e => e.QrImagePath).HasMaxLength(500);

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyChannels)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_SurveyChannels_Surveys");
        });

        modelBuilder.Entity<SurveyCollaborator>(entity =>
        {
            entity.HasKey(e => new { e.SurveyId, e.UserId });

            entity.Property(e => e.GrantedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyCollaborators)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_SurveyCollaborators_Surveys");

            entity.HasOne(d => d.User).WithMany(p => p.SurveyCollaborators)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_SurveyCollaborators_Users");
        });

        modelBuilder.Entity<SurveyResponse>(entity =>
        {
            entity.HasKey(e => e.ResponseId).HasName("PK__SurveyRe__1AAA646C5AFFADE2");

            entity.Property(e => e.ResponseId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AnonToken).HasMaxLength(200);
            entity.Property(e => e.AntiSpamToken).HasMaxLength(200);
            entity.Property(e => e.LastUpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RespondentEmail).HasMaxLength(255);
            entity.Property(e => e.RespondentIP).HasMaxLength(64);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Submitted");

            entity.HasOne(d => d.Channel).WithMany(p => p.SurveyResponses)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("FK_SurveyResponses_Channels");

            entity.HasOne(d => d.Survey).WithMany(p => p.SurveyResponses)
                .HasForeignKey(d => d.SurveyId)
                .HasConstraintName("FK_SurveyResponses_Surveys");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CC3149C2C");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534559FF87A").IsUnique();

            entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.ResetToken).HasMaxLength(200);
            entity.Property(e => e.UpdatedAtUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
