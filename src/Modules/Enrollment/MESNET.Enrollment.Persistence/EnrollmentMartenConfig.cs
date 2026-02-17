using Marten;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Persistence;

public class EnrollmentMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<StudentProfile>().DatabaseSchemaName("enrollment");
        options.Schema.For<StudentProfile>().Index(x => x.InstitutionId);
        options.Schema.For<StudentProfile>().Index(x => x.BranchCode);
        options.Schema.For<StudentProfile>().Index(x => x.KeycloakUserId);

        options.Schema.For<TeacherProfile>().DatabaseSchemaName("enrollment");
        options.Schema.For<TeacherProfile>().Index(x => x.InstitutionId);

        options.Schema.For<InternshipPlacement>().DatabaseSchemaName("enrollment");
        options.Schema.For<InternshipPlacement>().Index(x => x.StudentId);
        options.Schema.For<InternshipPlacement>().Index(x => x.BusinessId);

        options.Schema.For<BusinessProfileView>().DatabaseSchemaName("enrollment");
        options.Schema.For<BusinessProfileView>().Index(x => x.IsActive);
    }
}
