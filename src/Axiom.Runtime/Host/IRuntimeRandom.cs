namespace Axiom.Runtime;

public interface IRuntimeRandom
{
    double NextRandomDouble();

    // Оператор RANDOMIZE сеет источник заново: последующая последовательность
    // NextRandomDouble становится воспроизводимой для одного и того же seed.
    void Reseed(int seed);
}
