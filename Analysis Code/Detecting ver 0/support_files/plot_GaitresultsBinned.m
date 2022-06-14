function  plot_GaitresultsBinned(dataIN, cfg)
% helper function to plot either the distribution of all target onsets, or
% all response onsets (clicks), relative to gait "%"

% called from the script
% plot_ReactionTime_

GFX_headY = cfg.HeadData;
usecols = {[0 .7 0], [.7 0 0], [.7 0 .7]}; % R Gr, Prp

figure(1); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9 .9]);


if ~isfield(cfg, 'plotppants')
    plotppants = 1:size(dataIN);
else
    plotppants = cfg.plotppants;
end
nsubs = length(plotppants);
%% set up shortcuts for accessing data:
binfields = {'', '_binned'}; 
if strcmp(cfg.DV, 'RT')
    usefield = [binfields{cfg.usebin+1} '_rts'];
elseif strcmp(cfg.DV, 'Accuracy')
    usefield = [binfields{cfg.usebin+1} '_Acc'];
end

usegaitnames= {'gc','doubgc'};
%%

if strcmp(cfg.plotlevel, 'PFX')
    for ippant = 1:nsubs
        clf;
        pc=1; % plot counter
        pspots = [1,3,5, 2,4, 6]; %suplot order
        psubj= cfg.subjIDs{ippant}(1:2); % print ppid.
        % both this and the next use the same figure function:
        
        for nGaits_toPlot=1:2
            
            
            
            
            legp=[]; % for legend
            if nGaits_toPlot==1
                pidx= cfg.pidx1;
                ftnames= {'LR', 'RL', 'combined'};
            else
                pidx= cfg.pidx2;
                ftnames= {'LRL', 'RLR', 'combined'};
            end
            
            for iLR=1:3
                
                %                 [ppantData,plotHead,shuffData]=deal([]);
                
                
                
                ppantData= dataIN(ippant,iLR).([usegaitnames{nGaits_toPlot} usefield]);
                plotHead = GFX_headY(ippant).([usegaitnames{nGaits_toPlot}]);
                if cfg.plotShuff
                    shuffData(1:3,:) = quantile(dataIN(ippant,iLR).([usegaitnames{nGaits_toPlot} usefield '_shuff']), [.05 .5 .95]);
                end
                
                %x axis:          %approx centre point of the binns.
                mdiff = round(mean(diff(pidx)./2));
                xvec = pidx(1:end-1) + mdiff;
                
                %%
                subplot(3,2,pspots(pc))
                hold on;
                yyaxis left
                
                % finely sampled bar, each gait "%" point.
                bh=bar(xvec, ppantData);
                bh.FaceColor = usecols{iLR};
                legp(iLR)= bh;
                
                ylabel([cfg.type ' location  [ ' cfg.DV ']']);
                %                 ylim([ 0 .6]);
                
                yyaxis right
                ph=plot(plotHead, ['o'], 'linew', 3); hold on
                set(gca,'ytick', []);
                
                title([psubj '  ' ftnames{iLR}], 'interpreter', 'none');
                midp=xvec(ceil(length(xvec)/2));
                set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})
                
                xlabel([ '% of gait-cycle ']);%
                ylim([0 max(plotHead)]);
                pc=pc+1; %plotcounter
                
                
            end % i LR
            
        end % nGaits.
        %%
        cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' RT binned'])
        
        print([psubj ' ' cfg.type ' onset ' cfg.DV ' binned'],'-dpng');
    end % ppant
end
if strcmp(cfg.plotlevel, 'GFX')
    
    % plot mean effect
    
    clf;
    pc=1; % plot counter
    pspots = [1,3,5,2,4,6]; %suplot order
    psubj= 'GFX'; % print ppid.
    % both this and the next use the same figure function:
    
    
    for nGaits_toPlot=1:2
        
        
        legp=[]; % for legend
       
            if nGaits_toPlot==1
                pidx= cfg.pidx1;
                ftnames= {'LR', 'RL', 'combined'};
            else
                pidx= cfg.pidx2;
                ftnames= {'LRL', 'RLR', 'combined'};
            end
            
            if cfg.usebin
                
                %x axis:          %approx centre point of the binns.
                mdiff = round(mean(diff(pidx)./2));
                xvec = pidx(1:end-1) + mdiff;
            else  % note that if we aren't using the binned versions, use full xaxis.
                %         (overwrite the above)
                xvec = 1:pidx(end);
            end
        
        
        for iLR=1:3
            
            [ppantData,plotHead,shuffData]=deal([]);
            
            for isub= 1:length(plotppants)
                
                ppantData(isub,:)= dataIN(plotppants(isub),iLR).([usegaitnames{nGaits_toPlot} usefield]);
                plotHead(isub,:) = GFX_headY(plotppants(isub)).([usegaitnames{nGaits_toPlot}]);
                if cfg.plotShuff
                    shuffData(isub,1:3,:) = quantile(dataIN(plotppants(isub),iLR).([usegaitnames{nGaits_toPlot} usefield '_shuff']), [.05 .5 .95]);
                end
            end
            
            
           
            %%
            subplot(3,2,pspots(pc))
            hold on;
            %             yyaxis left
            
            % if normON
            if cfg.norm==1
                
                pM = nanmean(ppantData,2);
                meanVals= repmat(pM, 1, size(ppantData,2));
                
                
                if strcmp(cfg.normtype, 'absolute')
                    data = ppantData - meanVals;
                elseif strcmp(cfg.normtype, 'relative')
                    data = ppantData  ./ meanVals;
                    data=data-1;
                elseif strcmp(cfg.normtype, 'relchange')
                    data = (ppantData  - meanVals) ./ meanVals;
                elseif strcmp(cfg.normtype, 'normchange')
                    data = (ppantData  - meanVals) ./ (ppantData + meanVals);
                elseif strcmp(cfg.normtype, 'db')
                    data = 10*log10(ppantData  ./ meanVals);
                end
                
                ppantData= data;
                
            end
            gM = squeeze(nanmean(ppantData));
            stE = CousineauSEM(ppantData);
            % finely sampled bar, each gait "%" point.
            bh=bar(xvec, gM);
            hold on;
            errorbar(xvec, gM, stE, ...
                'color', 'k',...
                'linestyle', 'none',...
                'linew', 2);
            bh.FaceColor = usecols{iLR};
            legp(iLR)= bh;
            
            %adjust view to capture most of the variance:
              sdrange = max(gM) - min(gM);
                ylim([min(gM)-2*sdrange max(gM)+2*sdrange])
            
            %%  add best fourier fit
%            
%             %             best fit (use model params below):
%             [f,gof]= fit(xvec',gM',  'fourier1'); %unbounded
%             
%             % plot:
%             hold on;
%             h=plot(f, xvec, gM);%,
%             h(2).LineWidth = 2;
%             h(2).Color = 'k';
%             % include period and Rsquared
%             %treat max xvec as our full 'period'
%             Hzapp = xvec(end)/ (2*pi/(f.w));
%             %
%             legdetails = [sprintf('%.2f', Hzapp) ' Hz_G_C, R^2 = ' sprintf('%.2f', gof.rsquare) ];
%             legend(h(2), legdetails, 'fontsize', 15, 'autoupdate', 'off')
%             %
            %
            %% add shuff:
            %              %% also add shuffle data.
            %                 for iCV=[1,3]
            %                 tmp = squeeze( shuffData(:,iCV,:));
            %                    mP = nanmean(tmp,1);
            %                    stE= CousineauSEM(tmp);
            %                    shadedErrorBar(pidx(1:end-1), mP, stE, 'k');
            %                 end
            %             axis tight
            
            % plot head height?
            
                        plotHeadn = plotHead./ mean(gM);
%             rescale plotHead, so that the max, is above the ylim.
            plotHeadM = nanmean(plotHead,1);
%             norm to 1.
            plotHeadM = plotHeadM./max(plotHeadM);
            %adjust so max is (just) above ylim.
            plotHeadM= plotHeadM.* (cfg.ylims(2) + .1*diff(cfg.ylims));
            
            yyaxis right
            stEH= CousineauSEM(plotHead);
            shadedErrorBar(1:size(plotHead,2),plotHeadM, stEH,'k',1)
            
            
            set(gca,'ytick', []);
            %%
            title([psubj '  ' ftnames{iLR} ' N=' num2str(nsubs)], 'interpreter', 'none');
            midp=xvec(ceil(length(xvec)/2));
            set(gca,'fontsize', 15, 'xtick', [1, midp, xvec(end)], 'XTickLabels', {'0', '50', '100%'})
            
            xlabel([ cfg.type ' position as % of gait-cycle ']);%
            
            if strcmp(cfg.DV, 'RT')
                ylabel(['z( ' cfg.DV ')']);
            else
                ylabel( cfg.DV )
            end
            %                 ylim([cfg.ylims]);
            
            %adjust ylims to capture 2*SD of data points.
%             if cfg.norm==0 && strcmp(cfg.DV, 'RT')==0
                sdrange = max(gM) - min(gM);
                ylim([min(gM)-.5*sdrange max(gM)+1*sdrange])
%             else
%                 ylim(cfg.ylims)
%             end
            
            pc=pc+1; %plotcounter
            
            
        end % i LR
        
    end % nGaits.
    %%
    cd([cfg.datadir filesep  'Figures' filesep  cfg.type ' RT binned'])
    
    print([psubj ' ' cfg.type ' onset ' cfg.DV ' binned norm' num2str(cfg.norm)],'-dpng');
    
end% GFX
end