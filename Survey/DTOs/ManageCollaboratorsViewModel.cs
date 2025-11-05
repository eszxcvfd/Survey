namespace Survey.DTOs
{
    /// <summary>
    /// Main view model for the Manage Collaborators page
    /// </summary>
    public class ManageCollaboratorsViewModel
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string SurveyStatus { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
        public Guid OwnerId { get; set; }
        public List<CollaboratorViewModel> Collaborators { get; set; } = new List<CollaboratorViewModel>();
        public AddCollaboratorDto AddForm { get; set; } = new AddCollaboratorDto();
    }
}