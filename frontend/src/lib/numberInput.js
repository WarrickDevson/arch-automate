/**
 * Prevents non-numeric input for input fields when type="number" or for numeric validation.
 * @param {KeyboardEvent} e
 */
export const preventNonNumericKeydown = (e) => {
  // Allow: Backspace, Delete, Tab, Escape, Enter and .
  if (
    [46, 8, 9, 27, 13, 110, 190].indexOf(e.keyCode) !== -1 ||
    // Allow: Ctrl+A, Command+A
    (e.keyCode === 65 && (e.ctrlKey === true || e.metaKey === true)) ||
    // Allow: Ctrl+C, Command+C
    (e.keyCode === 67 && (e.ctrlKey === true || e.metaKey === true)) ||
    // Allow: Ctrl+V, Command+V
    (e.keyCode === 86 && (e.ctrlKey === true || e.metaKey === true)) ||
    // Allow: Ctrl+X, Command+X
    (e.keyCode === 88 && (e.ctrlKey === true || e.metaKey === true)) ||
    // Allow: home, end, left, right, down, up
    (e.keyCode >= 35 && e.keyCode <= 40)
  ) {
    // let it happen, don't do anything
    return
  }
  // Ensure that it is a number and stop the keypress
  if ((e.shiftKey || e.keyCode < 48 || e.keyCode > 57) && (e.keyCode < 96 || e.keyCode > 105)) {
    e.preventDefault()
  }
}
