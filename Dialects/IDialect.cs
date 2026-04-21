namespace GVisionWpf.Dialects
{
    public interface IDialect
    {
        public void Create<T>(string folderPath, string fileName, T entity);
        public T Read<T>(string folderPath, string fileName);
        public void Update<T>(string folderPath, string fileName, T entity);
        public void Delete<T>(string folderPath, string fileName);
    }
}

