namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcLongQueryTests : LongQueryTests<DatabaseRpc> {
    public RpcLongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestLongQueryTests : LongQueryTests<DatabaseRest> {
    public RestLongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class LongQueryTests <T> : MathQueryTests<T, long, long>
    where T : IDatabase, IDisposable, new() {

    private static IEnumerable<long> TestValues {
        get {
            yield return 10000; // Can't go too high otherwise the maths operations might overflow
            yield return 0;
            yield return -10000;
        }
    }

    public static IEnumerable<object[]> KeyAndValuePairs {
        get {
            return TestValues.Select(e => new object[] { RandomLong(), e });
        }
    }
    
    public static IEnumerable<object[]> KeyPairs {
        get {
            foreach (var testValue1 in TestValues) {
                foreach (var testValue2 in TestValues) {
                    yield return new object[] { testValue1, testValue2 };
                }
            }
        }
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static long RandomLong() {
        return ThreadRng.Shared.NextInt64();
    }

    protected override void AssertEquivalency(long a, long b) {
        b.Should().Be(a);
    }

    protected LongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
