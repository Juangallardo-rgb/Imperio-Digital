namespace SimuladorApi.DTOs.Courses
{
    public class ImportStudentsResultDto
    {
        public int TotalRows { get; set; }

        public int ValidRows { get; set; }

        public int NewUsersCreated { get; set; }

        public int ExistingStudentsEnrolled { get; set; }

        public int AlreadyEnrolled { get; set; }

        public int FailedRows { get; set; }

        public List<TemporaryCredentialDto> Credentials { get; set; } = new();

        public List<ImportStudentErrorDto> Errors { get; set; } = new();
    }

    public class TemporaryCredentialDto
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string TemporaryPassword { get; set; } = string.Empty;

        public string CourseCode { get; set; } = string.Empty;
    }

    public class ImportStudentErrorDto
    {
        public int RowNumber { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
