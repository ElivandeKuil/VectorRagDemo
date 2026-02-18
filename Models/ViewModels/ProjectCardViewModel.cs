using VectorRagDemo.Models.Entities.Management;
using VectorRagDemo.Services;

namespace VectorRagDemo.Models.ViewModels
{
    public class ProjectCardViewModel
    {
        public MgmtProject Project { get; set; } = null!;
        public ProjectInfo? RouteInfo { get; set; }
    }
}
