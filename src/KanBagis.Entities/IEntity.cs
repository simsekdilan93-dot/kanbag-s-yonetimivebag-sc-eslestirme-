namespace KanBagis.Entities;

/// <summary>Birincil anahtarı (Id) olan tüm varlıkların ortak arayüzü.</summary>
public interface IEntity
{
    int Id { get; set; }
}
