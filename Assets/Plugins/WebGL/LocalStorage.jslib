mergeInto(LibraryManager.library, {
  SaveToLocalStorage: function (keyPtr, valuePtr) {
    var key   = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);
    try {
      window.localStorage.setItem(key, value);
    } catch (e) {
      console.error("localStorage save failed", e);
    }
  },

  LoadFromLocalStorage: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var value = window.localStorage.getItem(key);
    if (value === null) value = "";
    var lengthBytes = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(lengthBytes);
    stringToUTF8(value, buffer, lengthBytes);
    return buffer;
  },

  RemoveFromLocalStorage: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    window.localStorage.removeItem(key);
  }
});