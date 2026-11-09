export const vnDate = (value?: string) => value ? new Date(value.length === 10 ? value + 'T00:00:00' : value).toLocaleDateString('vi-VN') : '—'
export const money = (value?: number) => value == null ? 'Liên hệ' : value === 0 ? 'Miễn phí' : value.toLocaleString('vi-VN') + ' đ'
export const today = () => new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date())
