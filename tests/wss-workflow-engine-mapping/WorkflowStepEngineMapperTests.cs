using Whycespace.Engines.T1M.WSS.Mapping;
using Whycespace.Engines.T1M.WSS.Stores;

namespace Whycespace.WSS.WorkflowEngineMapping.Tests;

public class WorkflowStepEngineMapperTests
{
    private readonly WorkflowEngineMappingStore _store;
    private readonly WorkflowStepEngineMapper _mapper;

    public WorkflowStepEngineMapperTests()
    {
        _store = new WorkflowEngineMappingStore();
        _mapper = new WorkflowStepEngineMapper(_store);
    }

    // 1. Register engine mapping
    [Fact]
    public void RegisterEngine_ShouldStoreMapping()
    {
        _mapper.RegisterEngine("RideCreationEngine", "engines.mobility.ride.create");

        Assert.True(_mapper.EngineExists("RideCreationEngine"));
    }

    // 2. Resolve engine mapping
    [Fact]
    public void ResolveEngine_RegisteredEngine_ShouldReturnRuntimeIdentifier()
    {
        _mapper.RegisterEngine("RideCreationEngine", "engines.mobility.ride.create");

        var result = _mapper.ResolveEngine("RideCreationEngine");

        Assert.Equal("engines.mobility.ride.create", result);
    }

    // 3. Detect missing engine mapping
    [Fact]
    public void ResolveEngine_MissingEngine_ShouldThrowEngineMappingException()
    {
        var ex = Assert.Throws<EngineMappingException>(() => _mapper.ResolveEngine("NonExistentEngine"));

        Assert.Equal("NonExistentEngine", ex.EngineName);
    }

    // 4. EngineExists validation
    [Fact]
    public void EngineExists_UnregisteredEngine_ShouldReturnFalse()
    {
        Assert.False(_mapper.EngineExists("UnknownEngine"));
    }

    [Fact]
    public void EngineExists_RegisteredEngine_ShouldReturnTrue()
    {
        _mapper.RegisterEngine("LedgerPostingEngine", "engines.finance.ledger.post");

        Assert.True(_mapper.EngineExists("LedgerPostingEngine"));
    }

    // 5. List engine mappings
    [Fact]
    public void ListEngines_ShouldReturnAllMappings()
    {
        _mapper.RegisterEngine("RideCreationEngine", "engines.mobility.ride.create");
        _mapper.RegisterEngine("LedgerPostingEngine", "engines.finance.ledger.post");
        _mapper.RegisterEngine("CapTableMutationEngine", "engines.finance.captable.mutate");

        var engines = _mapper.ListEngines();

        Assert.Equal(3, engines.Count);
        Assert.Equal("engines.mobility.ride.create", engines["RideCreationEngine"]);
        Assert.Equal("engines.finance.ledger.post", engines["LedgerPostingEngine"]);
        Assert.Equal("engines.finance.captable.mutate", engines["CapTableMutationEngine"]);
    }

    // 6. Duplicate registration should throw
    [Fact]
    public void RegisterEngine_DuplicateEngine_ShouldThrow()
    {
        _mapper.RegisterEngine("RideCreationEngine", "engines.mobility.ride.create");

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.RegisterEngine("RideCreationEngine", "engines.mobility.ride.create.v2"));
    }

    // 7. Empty list when no engines registered
    [Fact]
    public void ListEngines_NoEngines_ShouldReturnEmpty()
    {
        var engines = _mapper.ListEngines();

        Assert.Empty(engines);
    }
}
