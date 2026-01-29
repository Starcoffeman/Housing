namespace HousingManagementApp
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public partial class HousingStockManagementDBEntities : DbContext
    {
        // Статический экземпляр больше не нужен
        // private static HousingStockManagementDBEntities _instance;
        // private static readonly object _lock = new object();

        public HousingStockManagementDBEntities()
            : base("name=HousingStockManagementDBEntities")
        {
            // Отключаем отслеживание изменений для повышения производительности
            Configuration.AutoDetectChangesEnabled = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        // Новый метод для получения нового контекста
        public static HousingStockManagementDBEntities GetNewContext()
        {
            return new HousingStockManagementDBEntities();
        }

        // Старый метод GetContext() - удаляем или переименовываем
        public static HousingStockManagementDBEntities GetContext()
        {
            // Просто возвращаем новый контекст
            return new HousingStockManagementDBEntities();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }

        public virtual DbSet<Arrears> Arrears { get; set; }
        public virtual DbSet<City> City { get; set; }
        public virtual DbSet<HouseNumber> HouseNumber { get; set; }
        public virtual DbSet<HousingStock> HousingStock { get; set; }
        public virtual DbSet<Owner> Owner { get; set; }
        public virtual DbSet<Payments> Payments { get; set; }
        public virtual DbSet<Street> Street { get; set; }
    }
}