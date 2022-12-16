namespace AsterismCore {

public interface IRange<TRange> {
    TRange Intersect(TRange other);
}

}