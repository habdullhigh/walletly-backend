namespace SharedKernel;

public abstract class Entity
{
    // Parameterless constructor is for EF Core materialisation.
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; private set; }
}
