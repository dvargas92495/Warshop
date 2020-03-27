public delegate T ReturnAction<T>();
public delegate U ReturnAction<T, U>(T arg);
public delegate V ReturnAction<T, U, V>(T arg, U arg1);
public delegate W ReturnAction<T, U, V, W>(T arg, U arg1, V arg2);