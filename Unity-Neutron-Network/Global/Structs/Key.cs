using System;

[Serializable]
public struct Key {
    public object key;
    public object value;

    public Key (object key, object value) {
        this.key = key;
        this.value = value;
    }
}