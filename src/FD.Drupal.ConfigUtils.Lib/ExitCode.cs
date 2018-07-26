namespace FD.Drupal.ConfigUtils
{
    public enum ExitCode
    {
        Success = 0, 
        Unknown = 1, 
        UserCancelled = 2, 
        UnmetDependencies = 3,
        InvalidArguments = 100, 
        InvalidSourceDirectory = 101, 
        InvalidDestinationDirectory = 102, 
        IoError = 200
    }
}