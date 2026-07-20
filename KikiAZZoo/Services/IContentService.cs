using KikiAZZoo.Models;

namespace KikiAZZoo.Services
{
    /// <summary>
    /// Service để quản lý nội dung: BlogPosts, Services
    /// Tái sử dụng logic lặp lại giữa News, Service controllers
    /// </summary>
    public interface IContentService
    {
        Task<List<BlogPost>> GetAllBlogPostsAsync();
        Task<BlogPost?> GetBlogPostByIdAsync(int id);
        
        Task<List<Service>> GetAllServicesAsync();
        Task<Service?> GetServiceByIdAsync(int id);
    }
}
