export function position(): Promise<GeolocationCoordinates> {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) { reject(new Error('Trình duyệt chưa hỗ trợ định vị.')); return }
    navigator.geolocation.getCurrentPosition(p => resolve(p.coords), error =>
      reject(new Error(error.code === 1 ? 'Hãy cấp quyền vị trí để tiếp tục.' : 'Chưa lấy được vị trí. Hãy thử lại ở nơi thông thoáng.')),
    { enableHighAccuracy: true, timeout: 16000, maximumAge: 0 })
  })
}
