import { useState } from 'react'
import { api, qs, upload } from '../../shared/api'
import { useAction, useResource } from '../../shared/hooks'
import { Empty, Feedback, LoadState, PageTitle, Status } from '../../shared/ui'
import type { Heritage, Page } from '../../shared/types'
import { Stars } from './HeritageReading'

type Entry = Heritage & { trangThai: string; banKinhCheckInM: number; saiSoGpsToiDaM: number }
type Review = { nguoiDungId: number; soSao: number; noiDung?: string; congKhai: boolean }

export function CultureCatalog() {
  const [page,setPage]=useState(1), [selected,setSelected]=useState<number>()
  const list=useResource<Page<Entry>>('/quan-tri/di-san'+qs({trang:page}))
  const place=list.data?.muc.find(x=>x.diSanId===selected)
  return <><PageTitle eyebrow="BAN VĂN HÓA" title="Biên tập di sản" text="Chỉnh hồ sơ, bổ sung ảnh và xử lý đánh giá." />
    <div className="culture-layout"><section className="surface"><h2>Hồ sơ di sản</h2><LoadState {...list} retry={list.reload} />
      {list.data?.muc.map(x=><button key={x.diSanId} className={selected===x.diSanId?'queue-row selected':'queue-row'} onClick={()=>setSelected(x.diSanId)}>
        <span>{x.ten}</span><Status value={x.trangThai} /></button>)}
      {!!list.data && list.data.tong>30 && <div className="pager"><button className="button quiet" disabled={page===1} onClick={()=>{setPage(page-1);setSelected(undefined)}}>Trước</button>
        <span>Trang {page}</span><button className="button quiet" disabled={page*30>=list.data.tong} onClick={()=>{setPage(page+1);setSelected(undefined)}}>Sau</button></div>}
    </section><div className="article-stack">{place ? <Editor key={place.diSanId} place={place} reload={list.reload} /> : <Empty title="Chọn di sản để biên tập." />}</div></div>
  </>
}
function Editor({place,reload}:{place:Entry;reload:()=>void}) {
  const endpoint='/quan-tri/di-san/'+place.diSanId
  const [page,setPage]=useState(1)
  const reviews=useResource<Page<Review>>(endpoint+'/danh-gia'+qs({trang:page}))
  const action=useAction(), reviewAction=useAction()
  return <>
    <section className="surface"><h2>{place.ten}</h2>
      <form className="form-stack" onSubmit={e=>{
        e.preventDefault();const form=e.currentTarget,f=new FormData(form)
        void action.run(async()=>{
          const cover=f.get('cover'),video=f.get('video')
          const anhBiaPath=cover instanceof File && cover.size?await upload(cover):place.anhBiaPath
          const videoPath=video instanceof File && video.size?await upload(video):f.has('removeVideo')?null:place.videoPath
          await api(endpoint,'PUT',{...place,ten:f.get('ten'),tomTat:f.get('tomTat'),noiDung:f.get('noiDung'),diaChi:f.get('diaChi'),
            viDo:Number(f.get('viDo')),kinhDo:Number(f.get('kinhDo')),trangThai:f.get('status'),anhBiaPath,videoPath})
          for(const name of ['cover','video']) {const input=form.elements.namedItem(name);if(input instanceof HTMLInputElement) input.value=''}
          reload()
        },'Đã cập nhật di sản.')
      }}>
        <label>Tên di sản<input name="ten" defaultValue={place.ten} maxLength={200} required /></label>
        <label>Địa chỉ<input name="diaChi" defaultValue={place.diaChi} maxLength={250} required /></label>
        <div className="form-grid"><label>Vĩ độ<input type="number" step="any" min="-90" max="90" name="viDo" defaultValue={place.viDo} required /></label>
          <label>Kinh độ<input type="number" step="any" min="-180" max="180" name="kinhDo" defaultValue={place.kinhDo} required /></label></div>
        <label>Tóm tắt<textarea name="tomTat" rows={3} defaultValue={place.tomTat} maxLength={1000} required /></label>
        <label>Lịch sử và thông tin chi tiết<textarea name="noiDung" rows={9} defaultValue={place.noiDung} maxLength={20000} />
          <small>Dùng ## trước tiêu đề, **nội dung** cho chữ đậm và cách dòng giữa các đoạn.</small></label>
        <div className="form-grid"><label>Thay ảnh bìa<input type="file" name="cover" accept="image/jpeg,image/png,image/webp" /></label>
          <label>Thay hoặc thêm video<input type="file" name="video" accept="video/mp4" /></label></div>
        {place.videoPath && <label className="check"><input type="checkbox" name="removeVideo" /> Gỡ video hiện tại</label>}
        <label>Trạng thái<select name="status" defaultValue={place.trangThai}><option value="NHAP">Bản nháp</option><option value="CONG_KHAI">Công khai</option><option value="AN">Ẩn</option></select></label>
        <button className="button primary" disabled={action.busy}>Lưu hồ sơ di sản</button>
      </form>
      <form className="form-stack block" onSubmit={e=>{
        e.preventDefault();const form=e.currentTarget,f=new FormData(form),image=f.get('image')
        if(!(image instanceof File)||!image.size)return
        void action.run(async()=>{
          await api(endpoint+'/media','POST',{anhPath:await upload(image),chuThich:f.get('caption'),thuTu:Number(f.get('order'))})
          form.reset()
        },'Đã thêm ảnh tư liệu.')
      }}><h3>Thêm ảnh tư liệu</h3><label>Ảnh<input type="file" name="image" accept="image/jpeg,image/png,image/webp" required /></label>
        <div className="form-grid"><label>Chú thích và nguồn<input name="caption" maxLength={250} required /></label>
          <label>Thứ tự<input type="number" name="order" min={1} max={32767} defaultValue={10} required /></label></div>
        <button className="button outline" disabled={action.busy}>Thêm ảnh</button></form>
      <Feedback error={action.error} message={action.message} />
    </section>
    <section className="surface"><h2>Kiểm duyệt đánh giá</h2><p>Gỡ khỏi trang công khai đối với nhận xét xúc phạm, spam hoặc vi phạm quy tắc cộng đồng.</p>
      <LoadState {...reviews} retry={reviews.reload} /><Feedback error={reviewAction.error} message={reviewAction.message} />
      {reviews.data?.tong===0 && <Empty title="Chưa có đánh giá." />}
      {reviews.data?.muc.map(x=><article className="record" key={x.nguoiDungId}><div className="inline"><strong>Thành viên #{x.nguoiDungId}</strong>
        <Stars value={x.soSao} /><Status value={x.congKhai?'CONG_KHAI':'AN'} /></div><p>{x.noiDung}</p>
        <button className="button outline" disabled={reviewAction.busy} onClick={()=>void reviewAction.run(async()=>{
          await api(endpoint+'/danh-gia/'+x.nguoiDungId+'/cong-khai','PUT',{congKhai:!x.congKhai});reviews.reload()
        },x.congKhai?'Đã gỡ khỏi trang công khai.':'Đã khôi phục đánh giá.')}>{x.congKhai?'Gỡ đánh giá khỏi trang':'Khôi phục đánh giá'}</button></article>)}
      {!!reviews.data && reviews.data.tong>20 && <div className="pager"><button className="button outline" disabled={page===1} onClick={()=>setPage(page-1)}>Trước</button>
        <span>Trang {page}</span><button className="button outline" disabled={page*20>=reviews.data.tong} onClick={()=>setPage(page+1)}>Sau</button></div>}
    </section>
  </>
}
